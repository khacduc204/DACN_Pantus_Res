using SlugGenerator;
using System;
namespace KD_Restaurant.Utilities


{
    public static class Function
    {
        public static string TitleSlugGenerationAlias(string title)
        {
            return SlugGenerator.SlugGenerator.GenerateSlug(title);
        }
    }
}