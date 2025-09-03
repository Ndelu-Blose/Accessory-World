Alright ✅ let's lay out **wireframe designs** for both **Customer Portal** and **Admin Dashboard**.
I'll keep it structured like UI mockups (not final visuals, but layouts you can directly convert into HTML/CSS or Figma).

**Last Updated:** January 2025 - Reflects current application state with improved product cards, pricing display, and enhanced UI components.

---

# 👤 Customer Portal Wireframes

### 1. **Login / Signup Page**

```
 ------------------------------------------------
|                  Accessory World               |
|------------------------------------------------|
|   [ Email Address ______________________ ]     |
|   [ Password __________________________ ]      |
|                                                |
|   [ Login Button ]                             |
|   [ Signup Button ]                            |
|   Forgot Password?                             |
 ------------------------------------------------
```

---

### 2. **Customer Dashboard (After Login)**

```
 ------------------------------------------------
| Navbar: [Home] [Shop] [Orders] [Profile] [Logout] |
|--------------------------------------------------|
|  Welcome, [Customer Name]                        |
|                                                  |
|  Quick Actions:                                  |
|   - View My Orders                               |
|   - Edit Profile                                 |
|   - Saved Items / Wishlist                       |
 ------------------------------------------------
```

---

### 3. **Profile Page**

```
 ------------------------------------------------
| Profile Information                             |
|-------------------------------------------------|
| Name:     [______________]                      |
| Email:    [______________] (non-editable)       |
| Phone:    [______________]                      |
| Address:  [______________]                      |
|                                               |
| [ Save Changes ]                               |
 ------------------------------------------------
```

---

### 4. **Order History & Status**

```
 ------------------------------------------------
| My Orders                                       |
|-------------------------------------------------|
| Order #12345 | Date: 10 Aug 2025 | Status: Shipped |
|   - iPhone 13 Pro x1                            |
|   - Charger Cable x2                            |
|   Total: R12,500                                |
|   [ View Details ] [ Track Order ]             |
|-------------------------------------------------|
| Order #12346 | Date: 22 Aug 2025 | Status: Pending |
|   - Samsung S23 Ultra x1                        |
|   Total: R18,000                                |
|   [ View Details ] [ Cancel Order ]            |
 ------------------------------------------------
```

### 5. **Products/Shop Page (NEW - Current Implementation)**

```
 ------------------------------------------------
| Home > Shop                                     |
|-------------------------------------------------|
| [Search ____________] [Category ▼] [Sort ▼]     |
|-------------------------------------------------|
| Filters:          | Product Grid (3-4 cols)     |
| □ Category        | ┌─────────┐ ┌─────────┐     |
| □ Brand           | │ [Image] │ │ [Image] │     |
| □ Price Range     | │ Brand   │ │ Brand   │     |
| □ In Stock        | │ Product │ │ Product │     |
| [Clear Filters]   | │ R2,850  │ │ R3,999  │     |
|                   | │[Add Cart]│ │[Add Cart]│     |
|                   | └─────────┘ └─────────┘     |
|                   | ┌─────────┐ ┌─────────┐     |
|                   | │ [Image] │ │ [Image] │     |
|                   | │ Brand   │ │ Brand   │     |
|                   | │ Product │ │ Product │     |
|                   | │ R1,499  │ │ R8,999  │     |
|                   | │[Add Cart]│ │[Add Cart]│     |
|                   | └─────────┘ └─────────┘     |
 ------------------------------------------------
```

---

# 🛠️ Admin Dashboard Wireframes

### 1. **Admin Dashboard Home**

```
 ------------------------------------------------
| Sidebar: [Dashboard] [Users] [Orders] [Inventory] [Reports] [Logout] |
|---------------------------------------------------------------------|
| Stats Overview:                                                     |
|   - Total Sales: R250,000                                           |
|   - Active Users: 1,200                                             |
|   - Pending Orders: 25                                              |
|   - Low Stock Alerts: 5                                             |
 ------------------------------------------------
```

---

### 2. **Inventory Management**

```
 ------------------------------------------------
| Inventory List                                   |
|-------------------------------------------------|
| Product        | Stock   | Price   | Actions    |
| iPhone 13 Pro  |   10    | R12,500 | [Edit][Del]|
| Samsung S23    |    5    | R18,000 | [Edit][Del]|
| Charger Cable  |   50    |   R300  | [Edit][Del]|
|-------------------------------------------------|
| [Add New Product]                               |
 ------------------------------------------------
```

---

### 3. **Orders Management**

```
 ------------------------------------------------
| Orders                                           |
|-------------------------------------------------|
| Order #12345 | Customer: John D | Status: Paid   |
|   Total: R12,500 | Date: 10 Aug 2025             |
|   [Update Status] [View Invoice]                 |
|-------------------------------------------------|
| Order #12346 | Customer: Sarah K | Status: Pending |
|   Total: R18,000 | Date: 22 Aug 2025             |
|   [Update Status] [Cancel Order]                 |
 ------------------------------------------------
```

---

### 4. **Reports**

```
 ------------------------------------------------
| Reports Dashboard                               |
|-------------------------------------------------|
| [Sales Report - Download CSV]                   |
| [User Report - Download CSV]                    |
| [Inventory Report - Download CSV]               |
 ------------------------------------------------
```

---

### 6. **Product Detail Page (Enhanced)**

```
 ------------------------------------------------
| Home > Shop > iPhone 15 Pro                    |
|-------------------------------------------------|
| [Product Images]    | Product Info              |
| ┌─────────────────┐ | iPhone 15 Pro             |
| │                 │ | Apple                     |
| │   Main Image    │ | ★★★★★ (4.8) 124 reviews   |
| │                 │ |                           |
| └─────────────────┘ | Price: R2,850             |
| [thumb][thumb][+]   | (was R3,200)              |
|                     |                           |
| Description:        | Quantity: [1] [+] [-]     |
| Lorem ipsum...      | [ Add to Cart ]           |
|                     | [ Add to Wishlist ♡ ]    |
|                     |                           |
| Specifications:     | Delivery: 2-3 days       |
| • Screen: 6.1"      | Pickup: Available         |
| • Storage: 128GB    |                           |
 ------------------------------------------------
```

⚡ These wireframes capture **all your requirements** plus **current implementations**:

* Customer must **login before purchase**.
* Customers get profile editing + order history + status.
* **Enhanced product browsing** with filters, search, and improved cards.
* **Professional product cards** with proper pricing display.
* **Responsive grid layout** for optimal viewing.
* Admins see sales, users, orders, inventory, and reports.


