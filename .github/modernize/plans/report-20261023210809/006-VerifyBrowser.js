// Runs the dormant callback in a stub only; does not enable it in the website.
const fs = require('fs');
const path = require('path');
const vm = require('vm');
const assert = require('assert');
const root = path.join(process.cwd(), 'IaaS2PaaSWeb', 'Caldova-Asset-Manager.Website');
let checks = 0;
function check(test, name) {
    assert.ok(test, name);
    checks++;
    console.log('PASS: ' + name);
}
const anchor = { attr(name, value) { this[name] = value; }, text(value) { this.title = value; } };
let starts = 0, ready = 0, selected;
function $(arg) {
    if (typeof arg === 'function') { ready++; arg(); return; }
    selected = arg;
    assert.strictEqual(arg, 'a#new-arrival');
    return anchor;
}
$.connection = {
    Announcement: { client: {} },
    hub: { start() { starts++; return { done(f) { f(); } }; } }
};
vm.runInNewContext(fs.readFileSync(path.join(root, 'Scripts', 'NewStoreArrivals.js'), 'utf8'),
    { $, console: { log() {} } });
check(ready === 1 && starts === 1, 'dormant script wires one ready/start when explicitly evaluated');
check(typeof $.connection.Announcement.client.announcement === 'function', 'wire hub Announcement / event announcement');
const payload = { Title: '<img src=x onerror=alert(1)> & title', Url: '/shop/Store/Details/42' };
$.connection.Announcement.client.announcement(payload);
check(selected === 'a#new-arrival', 'callback targets actual partial anchor');
check(anchor.href === payload.Url, 'Url exact casing and virtual-directory path retained');
check(anchor.title === payload.Title, 'Title exact casing, text sink not HTML');
const partial = fs.readFileSync(path.join(root, 'Views', 'Shared', '_Announcement.cshtml'), 'utf8');
check(partial.includes('id="new-arrival"'), 'callback anchor exists');
check(partial.includes('Model == null') && partial.includes('<text>hidden</text>'), 'initial hidden state unchanged');
const bundles = fs.readFileSync(path.join(root, 'App_Start', 'BundleConfig.cs'), 'utf8');
check(bundles.includes('//bundles.Add(new ScriptBundle("~/bundles/signalr")'), 'signalr bundle stays commented');
check(bundles.includes('//"~/Scripts/NewStoreArrivals.js"'), 'site bundle callback stays commented');
const layout = fs.readFileSync(path.join(root, 'Views', 'Shared', '_Layout.cshtml'), 'utf8');
check(layout.includes('@*@Scripts.Render("~/bundles/signalr")*@'), 'layout SignalR render stays commented');
check(layout.includes('@*<script src="/signalr/hubs"></script>*@'), 'layout generated proxy reference stays commented');
check(layout.includes('@Html.Partial("_Announcement", (Product)ViewBag.Product)'), 'server-rendered announcement remains');
// Inspect every executable project-owned browser/registration reference, excluding vendor scripts.
let executable = [];
function inspect(dir) {
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
        if (['bin', 'obj', 'node_modules'].includes(entry.name)) continue;
        const filename = path.join(dir, entry.name);
        if (entry.isDirectory()) { inspect(filename); continue; }
        if (!/\.(cshtml|cs|js)$/.test(filename)) continue;
        if (/jquery|modernizr|bootstrap|respond|knockout|MicrosoftAjax|WebForms|WebUIValidation/i.test(entry.name)) continue;
        const text = fs.readFileSync(filename, 'utf8')
            .replace(/@\*[\s\S]*?\*@/g, '').replace(/\/\*[\s\S]*?\*\//g, '').replace(/\/\/[^\r\n]*/g, '');
        if (/NewStoreArrivals\.js|bundles\/signalr|signalr\/hubs/i.test(text)) executable.push(path.relative(root, filename));
    }
}
inspect(root);
check(executable.length === 0, 'no executable browser registration enables disabled real-time UI');
console.log('BROWSER CONTRACT CHECKS PASSED: ' + checks);
