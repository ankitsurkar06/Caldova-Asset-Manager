using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using CaldovaAssetManager.Models;
using CaldovaAssetManager.Security;

namespace CaldovaAssetManager.Controllers;

[Authorize]
public class AccountController(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn,
    AccountMessageSender messages) : Controller
{
    [AllowAnonymous]
    public async Task<IActionResult> Login(string returnUrl)
    {
        await SetLoginProviders(returnUrl);
        return View();
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl)
    {
        if (ModelState.IsValid)
        {
            var result = await signIn.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded) return RedirectToLocal(returnUrl);
            if (result.IsLockedOut) return View("Lockout");
            if (result.RequiresTwoFactor)
                return RedirectToAction(nameof(SendCode), new { returnUrl, rememberMe = model.RememberMe });
            ModelState.AddModelError("", "Invalid login attempt.");
        }
        await SetLoginProviders(returnUrl);
        return View(model);
    }

    [AllowAnonymous]
    public IActionResult Register() => View();

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
        var result = await users.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(model);
        }
        var code = await users.GenerateEmailConfirmationTokenAsync(user);
        var url = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, code }, Request.Scheme);
        await messages.SendEmailAsync(user.Email, "Confirm your account",
            $"Please confirm your account by clicking <a href=\"{System.Net.WebUtility.HtmlEncode(url)}\">here</a>.");
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string userId, string code)
    {
        if (userId == null || code == null) return View("Error");
        var user = await users.FindByIdAsync(userId);
        if (user == null) return View("Error");
        var result = await users.ConfirmEmailAsync(user, code);
        return View(result.Succeeded ? "ConfirmEmail" : "Error");
    }

    [AllowAnonymous]
    public IActionResult ForgotPassword() => View();

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.FindByEmailAsync(model.Email);
        if (user != null && await users.IsEmailConfirmedAsync(user))
        {
            var code = await users.GeneratePasswordResetTokenAsync(user);
            var url = Url.Action(nameof(ResetPassword), "Account", new { code }, Request.Scheme);
            await messages.SendEmailAsync(user.Email, "Reset Password",
                $"Please reset your password by clicking <a href=\"{System.Net.WebUtility.HtmlEncode(url)}\">here</a>.");
        }
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation() => View();

    [AllowAnonymous]
    public IActionResult ResetPassword(string code) => code == null ? View("Error") : View();

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.FindByEmailAsync(model.Email);
        if (user == null) return RedirectToAction(nameof(ResetPasswordConfirmation));
        var result = await users.ResetPasswordAsync(user, model.Code, model.Password);
        if (result.Succeeded) return RedirectToAction(nameof(ResetPasswordConfirmation));
        AddErrors(result);
        return View(model);
    }

    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation() => View();

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalLogin(string provider, string returnUrl)
    {
        if (!(await signIn.GetExternalAuthenticationSchemesAsync()).Any(p => p.Name == provider))
            return BadRequest("Unknown authentication provider.");
        var url = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        return Challenge(signIn.ConfigureExternalAuthenticationProperties(provider, url), provider);
    }

    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string returnUrl)
    {
        var info = await signIn.GetExternalLoginInfoAsync();
        if (info == null) return RedirectToAction(nameof(Login));
        var result = await signIn.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false);
        if (result.Succeeded) return RedirectToLocal(returnUrl);
        if (result.IsLockedOut) return View("Lockout");
        if (result.RequiresTwoFactor)
            return RedirectToAction(nameof(SendCode), new { returnUrl, rememberMe = false });
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.LoginProvider = info.LoginProvider;
        return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel
        {
            Email = info.Principal.FindFirstValue(ClaimTypes.Email),
            Name = info.Principal.FindFirstValue(ClaimTypes.Name)
        });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl = null)
    {
        if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Manage");
        if (ModelState.IsValid)
        {
            var info = await signIn.GetExternalLoginInfoAsync();
            if (info == null) return View("ExternalLoginFailure");
            var user = new ApplicationUser { UserName = model.Email, Email = model.Email, Name = model.Name };
            var result = await users.CreateAsync(user);
            if (result.Succeeded)
            {
                result = await users.AddLoginAsync(user, info);
                if (result.Succeeded)
                {
                    await signIn.SignInAsync(user, false);
                    return RedirectToLocal(returnUrl);
                }
            }
            AddErrors(result);
        }
        ViewBag.ReturnUrl = returnUrl;
        return View(model);
    }

    [AllowAnonymous]
    public async Task<IActionResult> SendCode(string returnUrl, bool rememberMe)
    {
        var user = await signIn.GetTwoFactorAuthenticationUserAsync();
        if (user == null) return View("Error");
        var providers = await users.GetValidTwoFactorProvidersAsync(user);
        return View(new SendCodeViewModel
        {
            Providers = providers.Select(p => new SelectListItem(p, p)).ToList(),
            ReturnUrl = returnUrl,
            RememberMe = rememberMe
        });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> SendCode(SendCodeViewModel model)
    {
        var user = await signIn.GetTwoFactorAuthenticationUserAsync();
        if (user == null) return View("Error");
        var providers = await users.GetValidTwoFactorProvidersAsync(user);
        if (!providers.Contains(model.SelectedProvider)) return BadRequest("Invalid two-factor provider.");
        if (model.SelectedProvider != TokenOptions.DefaultEmailProvider)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "SMS delivery is not configured. Configure an account messaging provider before using phone verification.");
        var code = await users.GenerateTwoFactorTokenAsync(user, model.SelectedProvider);
        await messages.SendEmailAsync(user.Email, "Security Code", $"Your security code is: {code}");
        return RedirectToAction(nameof(VerifyCode), new
        {
            provider = model.SelectedProvider, returnUrl = model.ReturnUrl, rememberMe = model.RememberMe
        });
    }

    [AllowAnonymous]
    public async Task<IActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
    {
        if (await signIn.GetTwoFactorAuthenticationUserAsync() == null) return View("Error");
        return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await signIn.TwoFactorSignInAsync(model.Provider, model.Code, model.RememberMe, model.RememberBrowser);
        if (result.Succeeded) return RedirectToLocal(model.ReturnUrl);
        if (result.IsLockedOut) return View("Lockout");
        ModelState.AddModelError("", "Invalid code.");
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LogOff()
    {
        await signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => StatusCode(StatusCodes.Status403Forbidden);

    private IActionResult RedirectToLocal(string returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Home");

    private async Task SetLoginProviders(string returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.AuthenticationSchemes = await signIn.GetExternalAuthenticationSchemesAsync();
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
    }
}
