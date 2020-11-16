﻿using AutoMapper;
using ImageMagick;
using LanguageExt;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SACA.Data;
using SACA.Interfaces;
using SACA.Models;
using SACA.Models.Identity;
using SACA.Models.Requests;
using SACA.Models.Responses;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SACA.Controllers
{
    public class ImagesController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IImageService _imageService;
        private readonly IMapper _mapper;

        public ImagesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IImageService imageService, IMapper mapper)
        {
            _context = context;
            _userManager = userManager;
            _imageService = imageService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<ImageResponse>> Get()
        {
            var userId = int.Parse(_userManager.GetUserId(User));

            var image = await _context.Images
                .Include(x => x.User)
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .FirstOrDefaultAsync();

            return OptionUnsafe<Image>.Some(image).MatchUnsafe<ActionResult<ImageResponse>>(image => Ok(_mapper.Map<ImageResponse>(image)), () => NotFound());
        }

        [HttpGet("{id}")]
        public async Task<ImageResponse> Get(int id)
        {
            var userId = int.Parse(_userManager.GetUserId(User));

            var image = await _context.Images
                .Include(x => x.User)
                .AsNoTracking()
                .Where(x => x.Id == id && x.UserId == userId)
                .FirstOrDefaultAsync();

            return _mapper.Map<ImageResponse>(image);
        }

        [HttpPost]
        public async Task<ImageResponse> Create(ImageRequest imageRequest)
        {
            var image = _mapper.Map<Image>(imageRequest);

            image.UserId = int.Parse(_userManager.GetUserId(User));
            var user = await _context.Users.Include(x => x.Categories).FirstOrDefaultAsync(x => x.Id == image.UserId);

            using var magickImage = new MagickImage(Convert.FromBase64String(imageRequest.Base64));
            magickImage.Resize(110, 150);

            imageRequest.Base64 = magickImage.ToBase64();

            (image.FullyQualifiedPublicUrl, image.Url) = await _imageService.UploadToCloudinaryAsync(imageRequest, user.Id);

            await _context.Images.AddAsync(image);

            var category = await _context.Categories.Include(x => x.ApplicationUsers).FirstOrDefaultAsync(x => x.Id == image.CategoryId);

            var userCategoryExists = user.Categories.Any(x => x.Id == category.Id);
            if (!userCategoryExists)
            {
                category.ApplicationUsers.Add(user);
            }

            await _context.SaveChangesAsync();

            return _mapper.Map<ImageResponse>(image);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ImageResponse>> Update(int id, ImageRequest imageRequest)
        {
            var originalImage = await _context.Images.FirstOrDefaultAsync(c => c.Id == id);
            if (originalImage is null || originalImage.Id != imageRequest.Id)
            {
                return Forbid();
            }

            var user = await _userManager.GetUserAsync(User);

            await _imageService.RemoveImageFromCloudinaryAsync(originalImage);

            using var magickImage = new MagickImage(Convert.FromBase64String(imageRequest.Base64));
            magickImage.Resize(110, 150);

            imageRequest.Base64 = magickImage.ToBase64();

            var image = _mapper.Map<Image>(originalImage);
            image.Name = imageRequest.Name;

            (image.FullyQualifiedPublicUrl, image.Url) = await _imageService.UploadToCloudinaryAsync(imageRequest, user.Id);

            _context.Entry(image).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return _mapper.Map<ImageResponse>(image);
        }

        [HttpDelete("{id}")]
        public async Task<ImageResponse> Remove(int id)
        {
            var image = await _context.Images.FirstOrDefaultAsync(c => c.Id == id);
            if (image is null)
            {
                return null;
            }

            await _imageService.RemoveImageFromCloudinaryAsync(image);

            _context.Remove(image);
            await _context.SaveChangesAsync();

            return _mapper.Map<ImageResponse>(image);
        }

        [HttpPost("superuser")]
        public async Task<ImageResponse> CreateAdmin(ImageRequest imageRequest)
        {
            var image = _mapper.Map<Image>(imageRequest);

            using var magickImage = new MagickImage(Convert.FromBase64String(imageRequest.Base64));
            imageRequest.Base64 = _imageService.Resize(magickImage, 110, 150).ToBase64();

            (image.FullyQualifiedPublicUrl, image.Url) = await _imageService.UploadToCloudinaryAsync(imageRequest, userId: null);

            await _context.Images.AddAsync(image);
            await _context.SaveChangesAsync();

            return _mapper.Map<ImageResponse>(image);
        }

        [HttpDelete("superuser/{id}")]
        public async Task<ImageResponse> RemoveAdmin(int id)
        {
            var image = await _context.Images.FirstOrDefaultAsync(c => c.Id == id);
            if (image is null)
            {
                return null;
            }

            await _imageService.RemoveImageFromCloudinaryAsync(image);

            _context.Remove(image);
            await _context.SaveChangesAsync();

            return _mapper.Map<ImageResponse>(image);
        }
    }
}
