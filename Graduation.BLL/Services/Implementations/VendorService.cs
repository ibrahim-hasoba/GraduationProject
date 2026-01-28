using Graduation.API.DTOs.Vendor;
using Graduation.API.Errors;
using Graduation.BLL.Services.Interfaces;
using Graduation.DAL.Data;
using Graduation.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Graduation.BLL.Services.Implementations
{
    public class VendorService : IVendorService
    {
        private readonly DatabaseContext _context;

        public VendorService(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<VendorDto> RegisterVendorAsync(string userId, VendorRegisterDto dto)
        {
            // Check if user already has a vendor account
            var existingVendor = await _context.Vendors
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (existingVendor != null)
                throw new ConflictException("You already have a vendor account");

            // Check if store name is unique
            var storeNameExists = await _context.Vendors
                .AnyAsync(v => v.StoreName.ToLower() == dto.StoreName.ToLower());

            if (storeNameExists)
                throw new ConflictException("Store name already exists. Please choose a different name");

            var vendor = new Vendor
            {
                UserId = userId,
                StoreName = dto.StoreName,
                StoreNameAr = dto.StoreNameAr,
                StoreDescription = dto.StoreDescription,
                StoreDescriptionAr = dto.StoreDescriptionAr,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                City = dto.City,
                Governorate = dto.Governorate,
                LogoUrl = dto.LogoUrl,
                BannerUrl = dto.BannerUrl,
                IsApproved = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            return await GetVendorByIdAsync(vendor.Id);
        }

        public async Task<VendorDto> GetVendorByIdAsync(int id)
        {
            var vendor = await _context.Vendors
                .Include(v => v.User)
                .Include(v => v.Products)
                .Include(v => v.Orders)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vendor == null)
                throw new NotFoundException("Vendor", id);

            return MapToDto(vendor);
        }

        public async Task<VendorDto?> GetVendorByUserIdAsync(string userId)
        {
            var vendor = await _context.Vendors
                .Include(v => v.User)
                .Include(v => v.Products)
                .Include(v => v.Orders)
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (vendor == null)
                return null;

            return MapToDto(vendor);
        }

        public async Task<IEnumerable<VendorListDto>> GetAllVendorsAsync(bool? isApproved = null)
        {
            var query = _context.Vendors
                .Include(v => v.Products)
                .AsQueryable();

            if (isApproved.HasValue)
                query = query.Where(v => v.IsApproved == isApproved.Value);

            var vendors = await query
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            return vendors.Select(v => new VendorListDto
            {
                Id = v.Id,
                StoreName = v.StoreName,
                StoreNameAr = v.StoreNameAr,
                LogoUrl = v.LogoUrl,
                City = v.City,
                Governorate = v.Governorate.ToString(),
                IsApproved = v.IsApproved,
                IsActive = v.IsActive,
                TotalProducts = v.Products.Count,
                CreatedAt = v.CreatedAt
            });
        }

        public async Task<VendorDto> UpdateVendorAsync(int id, string userId, VendorUpdateDto dto)
        {
            var vendor = await _context.Vendors
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vendor == null)
                throw new NotFoundException("Vendor", id);

            // Check if the user owns this vendor
            if (vendor.UserId != userId)
                throw new UnauthorizedException("You are not authorized to update this vendor");

            // Check if new store name conflicts with another vendor
            var storeNameExists = await _context.Vendors
                .AnyAsync(v => v.Id != id && v.StoreName.ToLower() == dto.StoreName.ToLower());

            if (storeNameExists)
                throw new ConflictException("Store name already exists. Please choose a different name");

            vendor.StoreName = dto.StoreName;
            vendor.StoreNameAr = dto.StoreNameAr;
            vendor.StoreDescription = dto.StoreDescription;
            vendor.StoreDescriptionAr = dto.StoreDescriptionAr;
            vendor.PhoneNumber = dto.PhoneNumber;
            vendor.Address = dto.Address;
            vendor.City = dto.City;
            vendor.Governorate = dto.Governorate;
            vendor.LogoUrl = dto.LogoUrl;
            vendor.BannerUrl = dto.BannerUrl;
            vendor.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetVendorByIdAsync(id);
        }

        public async Task<VendorDto> ApproveVendorAsync(int id, bool isApproved, string? rejectionReason = null)
        {
            var vendor = await _context.Vendors.FindAsync(id);

            if (vendor == null)
                throw new NotFoundException("Vendor", id);

            vendor.IsApproved = isApproved;
            vendor.UpdatedAt = DateTime.UtcNow;

            // TODO: You might want to send email notification here
            // await _emailService.SendVendorApprovalEmail(vendor, isApproved, rejectionReason);

            await _context.SaveChangesAsync();

            return await GetVendorByIdAsync(id);
        }

        public async Task<VendorDto> ToggleVendorStatusAsync(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);

            if (vendor == null)
                throw new NotFoundException("Vendor", id);

            vendor.IsActive = !vendor.IsActive;
            vendor.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetVendorByIdAsync(id);
        }

        public async Task DeleteVendorAsync(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);

            if (vendor == null)
                throw new NotFoundException("Vendor", id);

            _context.Vendors.Remove(vendor);
            await _context.SaveChangesAsync();
        }

        private VendorDto MapToDto(Vendor vendor)
        {
            return new VendorDto
            {
                Id = vendor.Id,
                UserId = vendor.UserId,
                UserEmail = vendor.User?.Email ?? string.Empty,
                UserFullName = $"{vendor.User?.FirstName} {vendor.User?.LastName}",
                StoreName = vendor.StoreName,
                StoreNameAr = vendor.StoreNameAr,
                StoreDescription = vendor.StoreDescription,
                StoreDescriptionAr = vendor.StoreDescriptionAr,
                LogoUrl = vendor.LogoUrl,
                BannerUrl = vendor.BannerUrl,
                PhoneNumber = vendor.PhoneNumber,
                Address = vendor.Address,
                City = vendor.City,
                Governorate = vendor.Governorate.ToString(),
                GovernorateId = (int)vendor.Governorate,
                IsApproved = vendor.IsApproved,
                IsActive = vendor.IsActive,
                TotalProducts = vendor.Products?.Count ?? 0,
                TotalOrders = vendor.Orders?.Count ?? 0,
                CreatedAt = vendor.CreatedAt,
                UpdatedAt = vendor.UpdatedAt
            };
        }
    }
}