using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CaldovaAssetManager.Models;

namespace CaldovaAssetManager.Controllers;

[Authorize]
public class ManageController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) : Controller
{
    public enum ManageMessageId
    {
        AddPhoneSuccess, AddLoginSuccess, ChangePasswordSuccess, SetTwoFactorSuccess,
        SetPasswordSuccess, RemoveLoginSuccess, RemovePhoneSuccess, Error
    }

    public async Task<IActionResult> Index(ManageMessageId? message)
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();
        ViewBag.StatusMessage = message switch
        {
            ManageMessageId.ChangePasswordSuccess => "Your password has been changed.",
            ManageMessageId.SetPasswordSuccess => "Your password has been set.",
            ManageMessageId.SetTwoFactorSuccess => "Your two-factor authentication provider has been set.",
            ManageMessageId.AddPhoneSuccess => "Your phone number was added.",
            ManageMessageId.RemovePhoneSuccess => "Your phone number was removed.",
            ManageMessageId.Error => "An error has occurred.",
            _ => ""
        };
        return View(new IndexViewModel
        {
            HasPassword = await users.HasPasswordAsync(user),
            PhoneNumber = user.PhoneNumber,
            TwoFactor = user.TwoFactorEnabled,
            Logins = await users.GetLoginsAsync(user),
            BrowserRemembered = await signIn.IsTwoFactorClientRememberedAsync(user)
        });
    }

    public IActionResult ChangePassword() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();
        var result = await users.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
        if (result.Succeeded)
        {
            await signIn.RefreshSignInAsync(user);
            return RedirectToAction(nameof(Index), new { message = ManageMessageId.ChangePasswordSuccess });
        }
        AddErrors(result);
        return View(model);
    }

    public IActionResult SetPassword() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();
        var result = await users.AddPasswordAsync(user, model.NewPassword);
        if (result.Succeeded)
        {
            await signIn.RefreshSignInAsync(user);
            return RedirectToAction(nameof(Index), new { message = ManageMessageId.SetPasswordSuccess });
        }
        AddErrors(result);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableTwoFactorAuthentication()
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();
        var result = await users.SetTwoFactorEnabledAsync(user, true);
        if (!result.Succeeded) return RedirectToAction(nameof(Index), new { message = ManageMessageId.Error });
        await signIn.RefreshSignInAsync(user);
        return RedirectToAction(nameof(Index), new { message = ManageMessageId.SetTwoFactorSuccess });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableTwoFactorAuthentication()
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();
        var result = await users.SetTwoFactorEnabledAsync(user, false);
        if (!result.Succeeded) return RedirectToAction(nameof(Index), new { message = ManageMessageId.Error });
        await signIn.RefreshSignInAsync(user);
        return RedirectToAction(nameof(Index), new { message = ManageMessageId.SetTwoFactorSuccess });
    }

    public IActionResult AddPhoneNumber() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult AddPhoneNumber(AddPhoneNumberViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        ModelState.AddModelError("", "SMS delivery is not configured. Configure a messaging provider before adding a phone number.");
        return View(model);
    }

    public IActionResult VerifyPhoneNumber(string phoneNumber) =>
        phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();
        var result = await users.ChangePhoneNumberAsync(user, model.PhoneNumber, model.Code);
        if (result.Succeeded)
        {
            await signIn.RefreshSignInAsync(user);
            return RedirectToAction(nameof(Index), new { message = ManageMessageId.AddPhoneSuccess });
        }
        AddErrors(result);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemovePhoneNumber()
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();
        var result = await users.SetPhoneNumberAsync(user, null);
        if (result.Succeeded) await signIn.RefreshSignInAsync(user);
        return RedirectToAction(nameof(Index), new
        {
            message = result.Succeeded ? ManageMessageId.RemovePhoneSuccess : ManageMessageId.Error
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RememberBrowser()
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();
        await signIn.RememberTwoFactorClientAsync(user);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgetBrowser()
    {
        await signIn.ForgetTwoFactorClientAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ManageLogins(ManageMessageId? message)
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();
        ViewBag.StatusMessage = message switch
        {
            ManageMessageId.RemoveLoginSuccess => "The external login was removed.",
            ManageMessageId.AddLoginSuccess => "The external login was added.",
            ManageMessageId.Error => "An error has occurred.",
            _ => ""
        };
        var logins = await users.GetLoginsAsync(user);
        ViewBag.ShowRemoveButton = user.PasswordHash != null || logins.Count > 1;
        return View(new ManageLoginsViewModel
        {
            CurrentLogins = logins,
            OtherLogins = (await signIn.GetExternalAuthenticationSchemesAsync())
                .Where(p => logins.All(l => l.LoginProvider != p.Name)).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveLogin(string loginProvider, string providerKey)
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();
        var logins = await users.GetLoginsAsync(user);
        if (user.PasswordHash == null && logins.Count <= 1)
            return RedirectToAction(nameof(ManageLogins), new { message = ManageMessageId.Error });
        var result = await users.RemoveLoginAsync(user, loginProvider, providerKey);
        if (result.Succeeded) await signIn.RefreshSignInAsync(user);
        return RedirectToAction(nameof(ManageLogins), new
        {
            message = result.Succeeded ? ManageMessageId.RemoveLoginSuccess : ManageMessageId.Error
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkLogin(string provider)
    {
        if (!(await signIn.GetExternalAuthenticationSchemesAsync()).Any(p => p.Name == provider))
            return BadRequest("Unknown authentication provider.");
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();
        var properties = signIn.ConfigureExternalAuthenticationProperties(
            provider, Url.Action(nameof(LinkLoginCallback)), user.Id);
        return Challenge(properties, provider);
    }

    public async Task<IActionResult> LinkLoginCallback()
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();
        var info = await signIn.GetExternalLoginInfoAsync(user.Id);
        var success = info != null && (await users.AddLoginAsync(user, info)).Succeeded;
        return RedirectToAction(nameof(ManageLogins), new
        {
            message = success ? ManageMessageId.AddLoginSuccess : ManageMessageId.Error
        });
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
    }
}
