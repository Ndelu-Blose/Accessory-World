You’ve got two separate blockers here:

---

# A) Razor build errors (RZ1031 on `<option>`)

This happens when Razor sees **C# mixed into the attribute list** of a Tag Helper element (here, the `<option>` inside a `<select asp-for="...">`). Typical patterns that trigger it:

```cshtml
<option @(Model.Filter=="Pending" ? "selected" : "") value="Pending">Pending</option>   // ❌ RZ1031
@* or *@
<option @if(Model.Filter=="Pending"){<text>selected</text>} value="Pending">Pending</option> // ❌ RZ1031
```

### Fix options (pick one, consistently):

**Fix 1 — Let MVC select the option automatically (best):**

* Keep `<select asp-for="Status">` or `asp-for="Filter"`.
* Render plain `<option value="...">...` without any `selected` logic.
* Make sure `Model.Status` (or `Model.Filter`) is set to the intended selected value in your GET action. MVC will mark that option selected on render.

```cshtml
<select asp-for="Status" class="form-select">
  <option value="">All</option>
  <option value="Pending">Pending</option>
  <option value="Paid">Paid</option>
  <option value="Shipped">Shipped</option>
</select>
```

**Fix 2 — Use `asp-items` (clean + DRY):**

* In the controller or view, build a `SelectList` (or use an enum):

```csharp
ViewBag.Statuses = new SelectList(new[]
{
  new { Value = "", Text = "All" },
  new { Value = "Pending", Text = "Pending" },
  new { Value = "Paid", Text = "Paid" },
  new { Value = "Shipped", Text = "Shipped" }
}, "Value", "Text", Model.Status);  // Model.Status = current selection
```

```cshtml
<select asp-for="Status" asp-items="ViewBag.Statuses" class="form-select"></select>
```

**Fix 3 — Keep conditional selection without TagHelper-violations:**
If you really must conditionally set `selected` inline, **don’t inject attributes**; instead, set the value of the `selected` attribute:

```cshtml
<select asp-for="Status" class="form-select">
  <option value="" selected="@(Model.Status == null ? "selected" : null)">All</option>
  <option value="Pending" selected="@(Model.Status=="Pending" ? "selected" : null)">Pending</option>
  <option value="Paid" selected="@(Model.Status=="Paid" ? "selected" : null)">Paid</option>
  <option value="Shipped" selected="@(Model.Status=="Shipped" ? "selected" : null)">Shipped</option>
</select>
```

Or precompute a token (no attributes injected):

```cshtml
@{ var sel = (string v) => Model.Status == v ? "selected" : null; }
<select asp-for="Status" class="form-select">
  <option value="" @sel(null)>All</option>
  <option value="Pending" @sel("Pending")>Pending</option>
  <option value="Paid" @sel("Paid")>Paid</option>
  <option value="Shipped" @sel("Shipped")>Shipped</option>
</select>
```

> Do this in all the files listed in the error (Orders.cshtml, OrderDetails.cshtml, PaymentConfirmations.cshtml, PickupOTP.cshtml, Products.cshtml, ShippingLabels.cshtml, Users.cshtml) where you used inline `@(...)` or `@if` in the **attribute area** of `<option>`.

---

# B) Duplicate `SettingsViewModel` (CS0101)

You now have **two classes named `SettingsViewModel` in the same namespace** `AccessoryWorld.ViewModels`.

### Fix steps:

1. **Find the original** (you likely created a second copy at `\ViewModels\SettingsViewModel.cs`).
2. **Delete the newer duplicate** file.
3. **Open the original** `SettingsViewModel` and add the missing props the views expect:

   ```csharp
   public string? PaypalClientId { get; set; }
   public string? PaypalClientSecret { get; set; }
   ```
4. Ensure the **Admin Settings view** and **AdminController** both bind to these properties (you already wired this, so syncing the single ViewModel class is the key).

---

# C) Trade-In “null form” root cause (recap)

From your logs:

* `Unable to apply configured form options since the request form has already been read.`
* Your custom logging middleware read the form before MVC.

You already adjusted the middleware, but double-check it’s exactly like this:

```csharp
app.Use(async (ctx, next) =>
{
    // Only log; don't consume form for MVC
    if (HttpMethods.IsPost(ctx.Request.Method) && ctx.Request.HasFormContentType)
    {
        ctx.Request.EnableBuffering();                 // <-- allow re-read
        var form = await ctx.Request.ReadFormAsync();  // safe read
        // log keys/files as needed here (do not modify body)
        ctx.Request.Body.Position = 0;                 // <-- reset for model binding
    }

    await next();
});
```

Also confirm Tag Helpers are active for `Views/`:

`Views/_ViewImports.cshtml`

```cshtml
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@using AccessoryWorld.Models.ViewModels
```

Once the Razor build errors are fixed and the duplicate ViewModel removed, your **Trade-In POST should finally bind values** (your log will show actual field values instead of `(null)`), and you’ll be redirected to Details on success instead of the form being cleared.

---

## Quick checklist to unblock you fast

* [ ] Replace conditional `<option>` patterns with one of the 3 safe fixes (do this in all files listed by RZ1031).
* [ ] Remove the duplicate `ViewModels/SettingsViewModel.cs`; update the original with `PaypalClientId` + `PaypalClientSecret`.
* [ ] Verify `_ViewImports.cshtml` under **Views/** (Tag Helpers on).
* [ ] Keep the **buffering** version of the logging middleware (and never replace the request body).
* [ ] Rebuild and submit the Trade-In form again.


