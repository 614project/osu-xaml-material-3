using System;
using osu.Framework;
using osu.Framework.Platform;

namespace Material3Gallery
{
    public static class Program
    {
        /// <summary>Options: <c>--page N</c> opens a page, <c>--dark</c> starts dark, <c>--seed N</c> picks a seed colour.</summary>
        public static void Main(string[] args)
        {
            var game = new GalleryGame();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--page" when i + 1 < args.Length:
                        game.StartPage = int.Parse(args[++i]);
                        break;

                    case "--dark":
                        game.StartDark = true;
                        break;

                    case "--seed" when i + 1 < args.Length:
                        game.StartSeed = int.Parse(args[++i]);
                        break;

                    case "--variant" when i + 1 < args.Length:
                        game.StartVariant = int.Parse(args[++i]);
                        break;
                }
            }

            using GameHost host = Host.GetSuitableDesktopHost("Material3Gallery");
            host.Run(game);
        }
    }
}
