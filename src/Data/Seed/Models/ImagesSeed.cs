﻿using AutoMapper;
using Newtonsoft.Json;
using SACA.Interfaces;
using SACA.Entities;
using SACA.Entities.Requests;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SACA.Data.Seed.Models
{
    public class ImagesSeed : EntitySeed<Image>
    {
        private readonly IImageService _imageService;
        private readonly IMapper _mapper;

        public ImagesSeed(ApplicationDbContext context, IImageService imageService, IMapper mapper) : base(context,
            "SACA.Data.Seed.Json.01Images.json")
        {
            _imageService = imageService;
            _mapper = mapper;
        }

        public override async Task LoadAsync()
        {
            var dbSet = _context.Set<Image>();

            if (!dbSet.Any())
            {
                var assembly = Assembly.GetExecutingAssembly();

                await using var stream = assembly.GetManifestResourceStream(RessourceName);
                using var reader = new StreamReader(stream, Encoding.UTF8);

                var json = await reader.ReadToEndAsync();
                var images = JsonConvert.DeserializeObject<List<Image>>(json);

                foreach (var image in images)
                {
                    var imageDto = _mapper.Map<ImageRequest>(image);
                    imageDto.Base64 = image.Url;

                    (image.FullyQualifiedPublicUrl, image.Url) =
                        await _imageService.UploadToCloudinaryAsync(imageDto);

                    await dbSet.AddAsync(image);
                }

                await _context.SaveChangesAsync();
            }
        }
    }
}