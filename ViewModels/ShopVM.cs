using AccessoryWorld.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AccessoryWorld.ViewModels
{
    public class ShopVM
    {
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public IEnumerable<Brand> Brands { get; set; } = new List<Brand>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        
        // Filter Properties
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public string? Condition { get; set; }
        public bool? InStock { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? IsOnSale { get; set; }
        public bool? IsBestSeller { get; set; }
        public bool? IsNew { get; set; }
        public bool? IsHot { get; set; }
        public bool? IsTodayDeal { get; set; }
        
        // Sorting Properties
        public string? SortBy { get; set; } = "name";
        public string? SortOrder { get; set; } = "asc";
        
        // Pagination Properties
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalProducts { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalProducts / PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        
        // Display Properties
        public string? ViewMode { get; set; } = "grid"; // grid or list
        public int ProductsPerRow { get; set; } = 4;
        
        // Filter Options for Dropdowns
        public List<SelectListItem> SortOptions => new List<SelectListItem>
        {
            new SelectListItem { Value = "name_asc", Text = "Name (A-Z)", Selected = SortBy == "name" && SortOrder == "asc" },
            new SelectListItem { Value = "name_desc", Text = "Name (Z-A)", Selected = SortBy == "name" && SortOrder == "desc" },
            new SelectListItem { Value = "price_asc", Text = "Price (Low to High)", Selected = SortBy == "price" && SortOrder == "asc" },
            new SelectListItem { Value = "price_desc", Text = "Price (High to Low)", Selected = SortBy == "price" && SortOrder == "desc" },
            new SelectListItem { Value = "newest", Text = "Newest First", Selected = SortBy == "newest" },
            new SelectListItem { Value = "bestseller", Text = "Best Sellers", Selected = SortBy == "bestseller" },
            new SelectListItem { Value = "sales_desc", Text = "Most Popular", Selected = SortBy == "sales" && SortOrder == "desc" }
        };
        
        public List<SelectListItem> PageSizeOptions => new List<SelectListItem>
        {
            new SelectListItem { Value = "12", Text = "12 per page", Selected = PageSize == 12 },
            new SelectListItem { Value = "24", Text = "24 per page", Selected = PageSize == 24 },
            new SelectListItem { Value = "36", Text = "36 per page", Selected = PageSize == 36 },
            new SelectListItem { Value = "48", Text = "48 per page", Selected = PageSize == 48 }
        };
        
        public List<SelectListItem> ConditionOptions => new List<SelectListItem>
        {
            new SelectListItem { Value = "", Text = "All Conditions", Selected = string.IsNullOrEmpty(Condition) },
            new SelectListItem { Value = "New", Text = "New", Selected = Condition == "New" },
            new SelectListItem { Value = "C.P.O", Text = "Certified Pre-Owned", Selected = Condition == "C.P.O" }
        };
        
        // Helper Methods
        public string GetCurrentFiltersAsQueryString()
        {
            var queryParams = new List<string>();
            
            if (!string.IsNullOrEmpty(SearchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(SearchTerm)}");
            if (CategoryId.HasValue)
                queryParams.Add($"categoryId={CategoryId}");
            if (BrandId.HasValue)
                queryParams.Add($"brandId={BrandId}");
            if (!string.IsNullOrEmpty(Condition))
                queryParams.Add($"condition={Uri.EscapeDataString(Condition)}");
            if (InStock.HasValue)
                queryParams.Add($"inStock={InStock}");
            if (MinPrice.HasValue)
                queryParams.Add($"minPrice={MinPrice}");
            if (MaxPrice.HasValue)
                queryParams.Add($"maxPrice={MaxPrice}");
            if (IsOnSale.HasValue)
                queryParams.Add($"isOnSale={IsOnSale}");
            if (IsBestSeller.HasValue)
                queryParams.Add($"isBestSeller={IsBestSeller}");
            if (IsNew.HasValue)
                queryParams.Add($"isNew={IsNew}");
            if (IsHot.HasValue)
                queryParams.Add($"isHot={IsHot}");
            if (IsTodayDeal.HasValue)
                queryParams.Add($"isTodayDeal={IsTodayDeal}");
            if (!string.IsNullOrEmpty(SortBy))
                queryParams.Add($"sortBy={Uri.EscapeDataString(SortBy)}");
            if (!string.IsNullOrEmpty(SortOrder))
                queryParams.Add($"sortOrder={Uri.EscapeDataString(SortOrder)}");
            if (PageSize != 12)
                queryParams.Add($"pageSize={PageSize}");
            if (!string.IsNullOrEmpty(ViewMode) && ViewMode != "grid")
                queryParams.Add($"viewMode={Uri.EscapeDataString(ViewMode)}");
                
            return queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        }
        
        public string GetPageUrl(int page)
        {
            var baseQuery = GetCurrentFiltersAsQueryString();
            var separator = baseQuery.Contains("?") ? "&" : "?";
            return $"{baseQuery}{separator}page={page}";
        }
        
        public bool HasActiveFilters()
        {
            return !string.IsNullOrEmpty(SearchTerm) ||
                   CategoryId.HasValue ||
                   BrandId.HasValue ||
                   !string.IsNullOrEmpty(Condition) ||
                   InStock.HasValue ||
                   MinPrice.HasValue ||
                   MaxPrice.HasValue ||
                   IsOnSale.HasValue ||
                   IsBestSeller.HasValue ||
                   IsNew.HasValue ||
                   IsHot.HasValue ||
                   IsTodayDeal.HasValue;
        }
        
        public int GetActiveFiltersCount()
        {
            int count = 0;
            if (!string.IsNullOrEmpty(SearchTerm)) count++;
            if (CategoryId.HasValue) count++;
            if (BrandId.HasValue) count++;
            if (!string.IsNullOrEmpty(Condition)) count++;
            if (InStock.HasValue) count++;
            if (MinPrice.HasValue || MaxPrice.HasValue) count++;
            if (IsOnSale.HasValue) count++;
            if (IsBestSeller.HasValue) count++;
            if (IsNew.HasValue) count++;
            if (IsHot.HasValue) count++;
            if (IsTodayDeal.HasValue) count++;
            return count;
        }
        
        // Pagination Helper Methods
        public IEnumerable<int> GetPageNumbers(int maxPages = 5)
        {
            var pages = new List<int>();
            var startPage = Math.Max(1, CurrentPage - maxPages / 2);
            var endPage = Math.Min(TotalPages, startPage + maxPages - 1);
            
            // Adjust start page if we're near the end
            if (endPage - startPage < maxPages - 1)
            {
                startPage = Math.Max(1, endPage - maxPages + 1);
            }
            
            for (int i = startPage; i <= endPage; i++)
            {
                pages.Add(i);
            }
            
            return pages;
        }
    }
}