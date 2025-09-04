

# Products page enhancement plan

## 1) Layout & grid

* **Container width:** max 1320px, centered, 24px side padding.
* **Columns:** 4-col desktop (≥1200px), 3-col laptop (≥992px), 2-col tablet (≥768px), 1-col mobile.
* **Gutters:** 24px desktop/laptop, 16px tablet/mobile.
* **Card height consistency:** lock an **image aspect ratio** (1:1 or 4:5), then let text areas auto-flow but cap with line-clamp to keep rows aligned.

## 2) Card anatomy (visual spec)

* **Card**: 12px radius, subtle shadow (e.g., 0 6px 18px rgba(0,0,0,.06)), 1px border #E9EDF2. Hover: lift by 2–4px + stronger shadow.
* **Zones (top→bottom):**

  1. **Image zone**: 1:1 (square) or 4:5, **object-fit: cover**, light gray background #F6F8FA. Optional **carousel dots** on hover for extra images.
  2. **Brand & title**: brand in small caps (12px, medium, #6B7280); title 14–16px semibold, 2-line clamp.
  3. **Price block (prominent)**:

     * Current price 18–20px bold.
     * If on sale: original price 14px muted + strikethrough; **discount badge** (e.g., “-10%”) in a pill.
  4. **Meta row**: stock badge (In Stock / Low Stock / Out of Stock), rating (★ 4.7 • 112), and color swatches if applicable.
  5. **Primary actions**:

     * **Add to Cart** (full-width, 44px height).
     * **Secondary**: “Details” (ghost), heart (wishlist), compare icon.
* **Badges**: “SALE”, “NEW”, “BESTSELLER”, “FLASH” — top-left, pill 10px radius, small bold text. Use meaningful colors (Sale=red, New=blue, Best=amber, Flash=purple).

## 3) Visual hierarchy & typography

* **Type scale**: 12 / 14 / 16 / 18 / 20 / 24.
* **Weights**: 600 for titles/prices, 500 for buttons, 400 body.
* **Spacing**: 8-point system (8/12/16/24). Inside card: 16px padding.
* **Color system** (example—adapt to your brand):

  * Primary: #0066FF (buttons/links)
  * Accent (Sale): #EF4444
  * Success (Stock): #10B981
  * Text: #0F172A (titles) / #475569 (meta)
  * Border: #E2E8F0 / Surface: #FFFFFF / Elevation: #F8FAFC

## 4) Action buttons (your current ones feel small)

* **Add to Cart**: full-width, 44–48px height, rounded-lg, bold label, cart icon on the left. Hover: slight tint + shadow; Disabled state for OOS.
* **Details**: outline/ghost style; keep hierarchy (primary > secondary).
* **Wishlist/Compare**: icon buttons top-right of card; show tooltip or label on hover.

## 5) Filters, sorting & utilities (page-level)

* **Top toolbar** (above the grid):

  * Result count (“128 items”) • **Sort** dropdown (Featured, Price ↑/↓, Newest, Best Rated) • View toggle (grid/list).
* **Left sidebar filters** (collapsible on mobile):

  * Category (with counts), Brand (top N + “More”), Price slider, Availability, Condition (New/Used), Color, Storage/Variant.
  * “Clear all” inline, and **applied filter chips** under the toolbar.
* **Pagination**: numbered + “Next”, or “Load more” with infinite scroll on mobile (but keep a footer for SEO).

## 6) Content polish for each card

* **Title**: Keep concise, push key variant (e.g., “128GB”) into the title or variant chip.
* **Price formatting**: Local currency prefix (“R”), fixed two decimals only where needed; if Sale, show **you-save** amount/percent.
* **Stock messaging**:

  * In Stock (green)
  * Low Stock (amber, “Only 3 left”)
  * Out of Stock (gray, disable Add to Cart, “Notify me” button)
* **Trust/UI icons** under actions (subtle row):

  * “Free Delivery over R1,000”, “12-mo warranty”, “7-day returns”.

## 7) Hover & micro-interactions

* **Image swap** on hover (alt view), quick-add qty stepper on desktop, gentle scale (1.02) for image only.
* **Wishlist**: toggle heart fill, little confetti/check micro-feedback.
* **Badge animation**: subtle fade-in, not bouncy.

## 8) Accessibility & usability

* **Tap targets** ≥44px height.
* **Focus states**: clear outlines for keyboard users.
* **Color contrast**: ensure AA at minimum (4.5:1 for text).
* **ARIA** labels for icons (Add to cart, Wishlist).
* **Skeleton loaders** for images/text on first load.

## 9) Performance & images

* Use **one consistent ratio** so rows line up.
* Serve **WebP/AVIF**, responsive sizes (1x/2x), lazy-load below the fold.
* Preload hero images, defer secondary JS.

## 10) Empty & edge states

* **Empty grid**: “No results for ‘Galaxy’” + suggested filters & a CTA to clear filters.
* **Error state**: simple retry with a short explanation.
* **Out of stock**: CTA becomes “Notify me”, collect email/phone.

## 11) Optional upgrades

* **Quick View modal** with large photos, variant picker, add-to-cart.
* **Compare** drawer (max 3 items).
* **Sticky mini-cart** icon with item count & peek on hover.
* **Promo strip** above grid (e.g., “Flash Sale ends in 02:15:23”).
