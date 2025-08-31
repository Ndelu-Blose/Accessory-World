Below is a **clear ready spec** that fully describes the landing page. I’ve included structure, copy, styles, data schema, and acceptance criteria. Where labels/taxonomy come from the live site, I cited them. you make and implement suggestions where needed for work to continue.

---

# Project: Accessory World — Landing Page (Exact Match)

## 1) Page Layout (top → bottom)

1. **Top strip (header utility bar)**

   * Left: “⚡ Flash Sale”
   * Right links: “Track Order”, “About”, “Contact”, “Blog”. ([accessoryworldza.com][1])
2. **Primary navigation bar**

   * Left logo: “ACCESSORY **WORLD**” (WORLD in magenta).
   * Menu: Home, About, FAQ, Trade Ins, Contact, **Shop** (dropdown with: Apple iPhone, Accessories, Macbook, iPads, Apple Watches). ([accessoryworldza.com][2])
   * Controls (same row): “Shop Categories” button, “All Categories” select, search input, **Wishlist**, **My Account**, **My Cart R0,00** (cart total shown as “R0,00” when empty). ([accessoryworldza.com][2])
3. **Hero section**

   * Left column:

     * Red pill badge: “BIG SALE”
     * H1: “Level Up Your Mobile Game”
     * Subline (muted): “IPHONES AT UNBEATABLE PRICES”
     * Price kicker: “From **R2850**” (the number in magenta)
     * Primary button: “SHOP NOW”
   * Right column: product composite image (iPhone + AirPods) on **blue gradient** background.
4. **Two-up promo tiles**

   * Left tile (coupon):

     * Overline: “USE CODE: **SALE35%**”
     * H2: “New iPads Stock Coming Soon”
     * Subline: “AMAZING DISCOUNTS AND DEALS”
     * CTA: “SHOP NOW”
     * Image: iPads.
   * Right tile (teaser):

     * Badge: “COMING SOON”
     * H2: “Gaming Accessories”
     * Link: “RELEASE DATE & PRICE”
     * Image: DualSense controller.
5. **Wide feature banner**

   * Overline: “EXCLUSIVE HEADPHONE”
   * H2: “Discounts On All Accessories”
   * CTA: “SHOP NOW”
   * Image: headphones/AirPods.
6. **Four promo cards (1 row, 4 columns)**

   * Card 1: Badge “NEW PRODUCT”, Title “New Gen iPads”, Sub “AVAILABLE ON PRE ORDER”, image.
   * Card 2: Badge “PRE ORDER NOW”, Title “Music Accessories”, Sub “PRE ORDER TODAY”, image.
   * Card 3: Badge “PRE ORDER NOW”, Title “Apple Watches”, Sub “STUDENT DISCOUNT”, image.
   * Card 4: Badge “MONTH DEAL”, Title “iPhone Winter Sale”, Sub “UP TO 35% OFF”, image.
7. **Tabbed product grid**

   * Section title (left): “Best Sellers”
   * Tabs (right): “Laptops”, “Iwatch” (click swaps dataset)
   * Product card: image (contain), name (bold), brand (muted), price (“R {price}”), button “Add to Cart”.
8. **Newsletter band**

   * H2: “Sign Up For Newsletter”
   * Email input + “SUBSCRIBE” button (inline).
9. **Footer**

   * Column 1 (brand block): logo, support email, phone, social icons.
   * Column 2 “Quick links”: About Us, Our Collection, iPhones, Accessories, Our Stores. ([accessoryworldza.com][2])
   * Column 3 “Support”: Support Center, Help Desk, Book A Repair, Trade Ins, Contact. ([accessoryworldza.com][2])
   * Column 4 “Order”: Check Order, Delivery & Pickup, Returns, Exchanges, FAQ’s. ([accessoryworldza.com][2])
   * Bottom row: payment badges strip; copyright “2025 © Accessory World | Developed by Computer Garden”.

> **Public contact context for seed values** (optional, if you need a realistic block):
> Phone **061 238 8527**; Address **379 Anton Lembede St, Doone House, Floor 3, Durban** (seen on their public social profiles tied to the domain). ([Facebook][3], [TikTok][4])

---

## 2) Component Inventory & Props (implementation-oriented)

### HeaderTopBar

* `links: [{label, href}]`
* Default: `[{Flash Sale,#}, {Track Order,#}, {About,/about}, {Contact,/contact}, {Blog,/blog}]`

### MainNavbar

* `logoSrc`, `logoAlt`, `menu: [{label, href, children?:[...] }]`, `showWishlist:boolean`, `showAccount:boolean`, `cartTotal:string`
* Default `menu`:
  `Home, About, FAQ, Trade Ins, Contact, Shop(children: Apple iPhone, Accessories, Macbook, iPads, Apple Watches)` ([accessoryworldza.com][2])

### CategorySearchBar

* `categories: string[]` (first item “All Categories”), `onSearch(term, category)`

### HeroBanner

* `badgeText="BIG SALE"`, `title`, `subtitle`, `priceLead`, `cta:{label,href}`, `image:{src,alt}`, `background=gradientBlue`

### PromoTile

* Overline (optional coupon), `title`, `subtitle?`, `badge?`, `cta?`, `link?`, `image`

### FeatureBanner

* Overline, `title`, `cta`, `image`

### PromoCard (x4)

* `badge`, `title`, `subtitle`, `image`

### ProductTabs

* `tabs:[{key,label}]` (default: BestSellers, Laptops, Iwatch)
* `productsByTab:{[key]: Product[]}`

### ProductCard

* `name`, `brand`, `price`, `image`, `onAddToCart(productId)`

### Newsletter

* `title="Sign Up For Newsletter"`, `onSubmit(email)`

### Footer

* `brand:{logoSrc, name}`, `contact:{email, phone, address?}`,
  `groups:[{title, links:[{label,href}]}]`, `paymentStripSrc`, `copyright`

---

## 3) Data Model (seed + runtime)

```json
{
  "Product": {
    "id": 1,
    "name": "iPhone 15 Pro",
    "brand": "Apple",
    "price": 2850,
    "imageUrl": "/images/iphone15pro.jpg",
    "category": "Apple iPhone",
    "isBestSeller": true
  },
  "CategoryList": ["All Categories","Apple iPhone","Accessories","Macbook","iPads","Apple Watches"]
}
```

**Tabs mapping (example):**

* `BestSellers`: `where isBestSeller == true` (max 8)
* `Laptops`: `where category in ["Macbook","Laptops"]` (max 8)
* `Iwatch`: `where category == "Apple Watches"` (max 8)

---

## 4) Visual System (CSS tokens)

```json
{
  "colors": {
    "primary": "#1399FF",
    "accentMagenta": "#FF37B9",
    "redChip": "#EF4444",
    "textDark": "#111111",
    "textMuted": "#6B7280",
    "border": "#E5E7EB",
    "cardBg": "#FFFFFF",
    "pageBg": "#F8FAFC",
    "heroGradient": "linear-gradient(105deg, #BFE0FF, #5AA7FF)"
  },
  "radii": { "card": 20, "pill": 999 },
  "shadow": "0 1px 4px rgba(0,0,0,0.06)",
  "font": { "family": "Inter, system-ui, -apple-system, Segoe UI, Roboto, Arial, sans-serif",
            "h1Weight": 800, "h2Weight": 800, "bodyWeight": 400 }
}
```

**Sizing & layout rules**

* Max content width: **1200px**; global horizontal padding **16px**.
* Hero: 2 columns (≈60/40), min-height **420px**, vertical padding **48px**.
* Cards: radius **20px**, shadow **subtle**, internal padding **16–24px**.
* Product images: fixed height **220px**, `object-fit: contain` (or `cover` for promos).
* Spacing between sections: **24–32px**.

**Responsive**

* ≤1024px: hero stacks (image below text), grids collapse 2 columns; footer columns stack 2×2.
* ≤640px: grids 1 column; reduce heading sizes by \~20%; hide wishlist count bubbles.

---

## 5) Copy Deck (exact strings)

* **Top strip:** Flash Sale · Track Order · About · Contact · Blog. ([accessoryworldza.com][1])
* **Hero:**

  * Badge: “BIG SALE”
  * H1: “Level Up Your Mobile Game”
  * Sub: “IPHONES AT UNBEATABLE PRICES”
  * Price kicker: “From **R2850**”
  * CTA: “SHOP NOW”
* **Promo 2-up:**

  * Left: “USE CODE: SALE35% · New iPads Stock Coming Soon · AMAZING DISCOUNTS AND DEALS · SHOP NOW”
  * Right: “COMING SOON · Gaming Accessories · RELEASE DATE & PRICE”
* **Feature banner:** “EXCLUSIVE HEADPHONE · Discounts On All Accessories · SHOP NOW”
* **4 promo cards:**

  1. “NEW PRODUCT · New Gen iPads · AVAILABLE ON PRE ORDER”
  2. “PRE ORDER NOW · Music Accessories · PRE ORDER TODAY”
  3. “PRE ORDER NOW · Apple Watches · STUDENT DISCOUNT”
  4. “MONTH DEAL · iPhone Winter Sale · UP TO 35% OFF”
* **Tabbed section heading:** “Best Sellers” (tabs: “Laptops”, “Iwatch”).
* **Newsletter:** “Sign Up For Newsletter · SUBSCRIBE”
* **Footer groups:**

  * **Quick links:** About Us · Our Collection · iPhones · Accessories · Our Stores
  * **Support:** Support Center · Help Desk · Book A Repair · Trade Ins · Contact
  * **Order:** Check Order · Delivery & Pickup · Returns · Exchanges · FAQ’s ([accessoryworldza.com][2])

---

## 6) Asset Requirements (file names and sizes)

* `/images/logo-aw.png` (transparent PNG, \~40×40 icon + wordmark)
* `/images/hero-main.png` (iPhone + AirPods composite, \~1200×800, ≤250KB)
* `/images/promo-ipads.jpg`, `/images/promo-gamepad.jpg` (≈1000×650, ≤220KB)
* `/images/feature-headphones.jpg`
* `/images/card-ipad.jpg`, `/images/card-music.jpg`, `/images/card-watch.jpg`, `/images/card-iphone-sale.jpg`
* `/images/payments-strip.png` (payment logos row)

> **Alt text** must describe the product (“iPhone 15 Pro and AirPods on gradient”).

---

## 7) Behaviors & Interactions

* **Search**: when submitted, call `onSearch(term, selectedCategory)`. No autocomplete required.
* **Tabs**: clicking a tab switches grid data (no page reload needed).
* **Add to Cart**: call `onAddToCart(productId)`; show toast “Added to cart”.
* **Wishlist**/**Account**/**Cart** icons: link stubs OK.
* **Newsletter**: on submit, validate email; POST to `/api/newsletter`.

---

## 8) Acceptance Criteria (testable)

1. The header contains the **exact** link labels specified; the “Shop” menu contains the 5 child items named above. ([accessoryworldza.com][2])
2. The hero shows the correct strings (“Level Up Your Mobile Game… From R2850”) and the price value is styled in magenta.
3. The two promo tiles render with their respective coupon/badge text and images.
4. The wide feature banner appears directly under the promos with CTA.
5. The four promo cards show the **exact** badges/titles/subtitles listed.
6. The product section title is “Best Sellers” and includes **two tabs**: Laptops, Iwatch.
7. Product cards display **image, name, brand, price, CTA**.
8. The newsletter band shows title + input + button in one row on desktop.
9. The footer shows 4 columns with the **exact** group titles and **exact** link labels listed. ([accessoryworldza.com][2])
10. At ≤640px width, the hero stacks, grids collapse to 1 column, and footer stacks; no horizontal scrollbars.

---

## 9) Example Data (8 items)

```json
{
  "products": [
    {"id":1,"name":"iPhone 15 Pro","brand":"Apple","price":2850,"imageUrl":"/images/iphone15pro.jpg","category":"Apple iPhone","isBestSeller":true},
    {"id":2,"name":"AirPods Pro (2nd Gen)","brand":"Apple","price":3999,"imageUrl":"/images/airpods2.jpg","category":"Accessories","isBestSeller":true},
    {"id":3,"name":"PS5 DualSense","brand":"Sony","price":1499,"imageUrl":"/images/dualsense.jpg","category":"Accessories","isBestSeller":true},
    {"id":4,"name":"MacBook Air 13 M2","brand":"Apple","price":18999,"imageUrl":"/images/macbookair13.jpg","category":"Macbook","isBestSeller":true},
    {"id":5,"name":"HP Envy 14","brand":"HP","price":16999,"imageUrl":"/images/hpenvy14.jpg","category":"Laptops","isBestSeller":false},
    {"id":6,"name":"Apple Watch Series 9","brand":"Apple","price":8999,"imageUrl":"/images/watchs9.jpg","category":"Apple Watches","isBestSeller":true},
    {"id":7,"name":"Apple Watch SE","brand":"Apple","price":6499,"imageUrl":"/images/watchse.jpg","category":"Apple Watches","isBestSeller":false},
    {"id":8,"name":"iPad Air (M2)","brand":"Apple","price":12999,"imageUrl":"/images/ipadair.jpg","category":"iPads","isBestSeller":true}
  ]
}
```

---

## 10) Generation Hints 

* **Generate semantic HTML** with BEMish classes (`.header-top`, `.navbar`, `.hero`, `.card`, `.product-card`, `.newsletter`, `.footer`).
* **Inject copy** verbatim from section 5.
* **Use tokens** from section 4; set **hero background** to `heroGradient`.
* **Ensure ARIA**: `role="navigation"`, `aria-label` for menus, `<nav>` landmarks, and `<button>` labels.
* **Load images** lazily (`loading="lazy"`) except hero.
* **No JS framework required**; tabs can be plain JS toggling `data-active` or class.

---



[1]: https://accessoryworldza.com/spark-your-savings-on-electronics-18/?utm_source=chatgpt.com "Spark Your Savings on Electronics! - Accessory World"

[3]: https://www.facebook.com/p/Accessory-World-100082994817663/?utm_source=chatgpt.com "Accessory-World | Durban"

Awesome — let’s zero in on the **Products (Shop) experience** for Accessory World and turn it into a precise, build-ready spec you can implement now.

I checked the live site structure and product templates so these labels and patterns line up with what’s there (menu taxonomy, cart strip, category set, etc.). ([accessoryworldza.com][1])

# Products Page (Category / Shop Listing) — Exact-Match Spec

## 1) Page goals

* Let users **discover and compare** iPhones & accessories quickly.
* Support **sorting, filtering, pagination**, and fast **add-to-cart**.
* Keep label/taxonomy consistent with site nav: **Apple iPhone, Accessories, Macbook, iPads, Apple Watches**. Also expose the “All Categories” quick picker that appears in the site chrome. ([accessoryworldza.com][1])

## 2) IA & URL patterns

* Base route: `/shop` (or `/products`) with optional query params:

  * `?q=` (search term), `?category=`, `?tags=`, `?min=`, `?max=`, `?sort=`, `?page=`, `?inStock=true`.
* Example: `/shop?category=Apple%20iPhone&sort=price-asc&page=2`.

## 3) Page structure (top → bottom)

1. **Breadcrumbs**: Home › Shop › {Category} (optional if searching).
2. **Heading**: “Shop” or “{Category}”.
3. **Toolbar (top)**:

   * Left: **Result count** (“Showing 1–12 of 128 results”).
   * Middle: **Sort select**:

     * Default sorting
     * Price: Low to High
     * Price: High to Low
     * Latest
     * Popularity
   * Right: **View toggle** (Grid/List) + **Products per page** (12 / 24 / 48).
4. **Layout**:

   * **Sidebar (left)**: filters.
   * **Grid (right)**: 3–4 columns desktop; 2 columns tablet; 1 mobile.
5. **Pagination**: Prev · 1 2 3 … · Next.
6. **SEO block (optional)**: a short paragraph under the grid for category SEO.

## 4) Filters (Sidebar)

* **Category** (multi-select):

  * Apple iPhone, Accessories, Macbook, iPads, Apple Watches (match site IA). ([accessoryworldza.com][1])
* **Price range** (min–max slider).
* **Brand** (Apple, Sony, etc.).
* **Condition** (New, C.P.O).
* **Availability** (In Stock only).
* **Tags** (e.g., Smartphone, Charger, Case).
* **Reset filters** button.

**Behavior**: Filters update results without full page reload (pushState). If you need to start static, reload is fine; keep query string canonicalized.

## 5) Product card spec (grid)

Each card shows:

* **Image** (contain; fixed box \~ 220px tall; lazy-loaded).
* **Title** (product name, 2-line clamp).
* **Brand** (muted).
* **Price** (`R` + amount; if discounted, show original struck-through + badge).
* **Badges** (optional): “Today’s Deal”, “Hot”, “New”, “Pre-Order”. (The “Today’s Deal / Hot” labels appear in their UI chrome on product pages.) ([accessoryworldza.com][1])
* **CTA**:

  * Primary: **Add to Cart** (button)
  * Secondary: **View** (link to PDP)
* **Quick actions** (optional): Wishlist ♡.

**Hover state** (desktop):

* Slight card lift + shadow increase.
* Swap image if alternate present.

## 6) Sorting rules

* `default`: feature score (best seller, freshness) or name asc if you don’t keep a feature score.
* `price-asc`, `price-desc`.
* `latest`: by `CreatedAt`/`PublishedAt` desc.
* `popularity`: by `SalesCount`/`Views` desc (fall back to name if missing).

## 7) Empty & no-results states

* **Empty catalog**: show 8 skeleton cards + “No products available right now.” + link to Contact.
* **No results (filtered)**: “No matches for your filters.” + **Clear filters** button (keeps search term).

## 8) Accessibility

* Grid uses `<ul role="list">` → each card `<li>` or `<article>`.
* Buttons vs links: **Add to Cart** is `<button>`; **View** is `<a>` to PDP.
* Filters are real form controls with labels; price slider also has numeric inputs.
* Pagination has `aria-current="page"` on active page.
* Announce result count changes (ARIA live region).

## 9) Performance

* **Lazy-load** all grid images (`loading="lazy"`; `decoding="async"`).
* Use width/height attributes to reserve space (CLS).
* Server-side paging (12/24/48) to keep payload small.

## 10) Data model deltas (beyond your current `Product`)

Add fields so sorting/badges work well:

```csharp
public class Product {
  public int Id { get; set; }
  public string Name { get; set; } = "";
  public string Brand { get; set; } = "";
  public decimal Price { get; set; }
  public decimal? CompareAtPrice { get; set; } // for strikethrough
  public string ImageUrl { get; set; } = "";
  public string Category { get; set; } = "";   // Apple iPhone, Accessories, Macbook, iPads, Apple Watches
  public bool IsBestSeller { get; set; }
  public bool IsNew { get; set; }
  public bool IsHot { get; set; }              // “Hot” badge
  public bool IsTodayDeal { get; set; }        // “Today’s Deal”
  public bool InStock { get; set; } = true;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public int SalesCount { get; set; }          // for popularity
  public string Condition { get; set; } = "New"; // New or C.P.O
  public List<string> Tags { get; set; } = new();
}
```

## 11) Controller actions (ASP.NET Core MVC)

```csharp
// GET /shop
public IActionResult Index(string? q, string? category, string? tags,
                           decimal? min, decimal? max, string? sort = "default",
                           bool inStock = false, int page = 1, int pageSize = 12)
{
    IQueryable<Product> qy = _db.Products.AsNoTracking();

    // search
    if (!string.IsNullOrWhiteSpace(q))
        qy = qy.Where(p => p.Name.Contains(q) || p.Brand.Contains(q));

    // category
    if (!string.IsNullOrWhiteSpace(category))
        qy = qy.Where(p => p.Category == category);

    // tags (comma-separated)
    if (!string.IsNullOrWhiteSpace(tags)) {
        var arr = tags.Split(',', StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
        qy = qy.Where(p => p.Tags.Any(t => arr.Contains(t)));
    }

    if (min.HasValue) qy = qy.Where(p => p.Price >= min.Value);
    if (max.HasValue) qy = qy.Where(p => p.Price <= max.Value);
    if (inStock)     qy = qy.Where(p => p.InStock);

    // sort
    qy = sort switch {
        "price-asc"  => qy.OrderBy(p => p.Price),
        "price-desc" => qy.OrderByDescending(p => p.Price),
        "latest"     => qy.OrderByDescending(p => p.CreatedAt),
        "popular"    => qy.OrderByDescending(p => p.SalesCount),
        _            => qy.OrderBy(p => p.Name)
    };

    var total = qy.Count();
    var items = qy.Skip((page-1)*pageSize).Take(pageSize).ToList();

    var vm = new ShopVM {
        Items = items,
        Total = total,
        Page = page,
        PageSize = pageSize,
        Query = q, Category = category, Tags = tags,
        Min = min, Max = max, Sort = sort, InStock = inStock,
        Categories = new [] {"Apple iPhone","Accessories","Macbook","iPads","Apple Watches"}
    };
    return View(vm);
}
```

`ShopVM` includes the filters, result count, pagination data, and the category list pulled from the site IA. ([accessoryworldza.com][1])

## 12) Razor for product card (essentials)

```cshtml
<article class="card product" data-id="@p.Id">
  <a class="product__img" asp-controller="Products" asp-action="Details" asp-route-id="@p.Id" aria-label="@p.Name">
    <img src="@p.ImageUrl" alt="@p.Name" width="320" height="220" loading="lazy" decoding="async" />
  </a>
  <div class="product__body">
    <h3 class="product__title">@p.Name</h3>
    <p class="product__brand text-muted">@p.Brand</p>

    <div class="product__price">
      @if (p.CompareAtPrice.HasValue && p.CompareAtPrice > p.Price) {
        <span class="price-current">R @p.Price</span>
        <span class="price-compare">R @p.CompareAtPrice</span>
      } else {
        <span class="price-current">R @p.Price</span>
      }
    </div>

    <div class="product__badges">
      @if (p.IsTodayDeal){<span class="badge badge--deal">Today’s Deal</span>}
      @if (p.IsHot){<span class="badge badge--hot">Hot</span>}
      @if (p.IsNew){<span class="badge badge--new">New</span>}
    </div>

    <div class="product__actions">
      <button class="btn" data-add="@p.Id">Add to Cart</button>
      <a class="link" asp-controller="Products" asp-action="Details" asp-route-id="@p.Id">View</a>
    </div>
  </div>
</article>
```

## 13) Seed data (to avoid an empty shop)

Seed 16+ items across **Apple iPhone** (multiple models incl. C.P.O), **Accessories** (AirPods, chargers, cases), **Macbook**, **iPads**, **Apple Watches**. Mark a few with `IsTodayDeal=true` or `IsHot=true` to drive badges. (The site markets iPhones heavily and uses “Today’s Deal / Hot” affordances on product chrome.) ([accessoryworldza.com][1])

## 14) Acceptance criteria (testable)

* **Sorting** switches order correctly (inspect first prices for asc/desc).
* **Filters** mutate query string and survive page refresh (deep-linkable).
* **Pagination** renders accurate counts and page links.
* **Badges** render when flags set (Today’s Deal / Hot / New).
* **Images** lazy-load, no layout shift.
* **Accessibility**: keyboard can reach filters, sort, grid, and pagination; active page has `aria-current`.
* **Performance**: page loads in < 3s, 90+ on mobile.
* **SEO**: meta tags, OG tags, and schema.org markup.
* **Cart**: items add, update, remove, and persist.
* **Checkout**: flow completes, order is created, and cart is cleared.
* **Payment**: accepts major cards, PayPal, and bank transfers.
* **Shipping**: calculates cost and tracks delivery.
* **Returns**: process completes, refund is issued, and customer is notified.
* **Customer account**: creates, updates, and deletes account.
* **Order history**: tracks past orders and status.
* **Product details**: includes images, description, price, and specs.
* **Category navigation**: filters products by category.
* **Search**: finds products by name or brand.
* **Wishlist**: adds, removes, and views products.
* **Compare**: adds, removes, and views products.
* **Newsletter**: signs up, unsubscribes, and receives updates.
* **Contact**: submits, receives, and responds to messages.
* **FAQs**: answers common questions.
* **Privacy policy**: explains data handling.
* **Terms of service**: outlines site usage.
* **Refund policy**: explains return process.
* **Shipping policy**: explains delivery options.
* **Payment methods**: lists accepted methods.
* **Customer support**: provides help and guidance.
* **Social media**: follows and interacts with brand.
* **Google Analytics**: tracks site traffic.
* **Google Search Console**: optimizes site for search.
* **Facebook Pixel**: tracks conversions.
* **Twitter**: follows and interacts with brand.
* **LinkedIn**: follows and interacts with brand.
* **Terms of service**: outlines
