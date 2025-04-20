using static System.Net.Mime.MediaTypeNames;
using System.IO;
using System;
using System.Drawing.Imaging;
using System.Drawing;
using ImageMagick;
using System.Threading;
using System.Xml.Linq;
using System.Collections.Generic;

namespace generator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var inputs = new List<string>();
            string path = @"C:\content\cabal\";
            string combined_path = @"C:\content\cabal\merged";

            if (!Directory.Exists(combined_path))
                Directory.CreateDirectory(combined_path);

            var files_list = Directory.GetFiles(path, "*.gif", SearchOption.TopDirectoryOnly);
            foreach (var filePath in files_list)
            {
                var name = Path.GetFileNameWithoutExtension(filePath);
                var outputDir = $"{path}{name}";
                inputs.Add(outputDir);
                Console.WriteLine(name);

                if (!Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                ExtractGif(filePath, outputDir);
            }

            var sample_name = Path.GetFileNameWithoutExtension(files_list[0]);
            var sample = Directory.GetFiles($"{path}{sample_name}");
            var sampleSize = sample.Length;
            string filename_part = "frame_000";
            string ext = ".png";

            for (var pos = 0; pos < sampleSize; pos++)
            {
                string count = pos.ToString();
                var filename = filename_part.Substring(0, filename_part.Length - count.Length);
                filename += count;
                filename += ext;

                var merged_output_path = Path.Combine(combined_path, filename);

                var input_files = new List<string>();

                foreach(var inputDir in inputs)
                {
                    input_files.Add(Path.Combine(inputDir, filename));
                }
                CombineImages(input_files.ToArray(), merged_output_path);
                Console.WriteLine( $"Merging {merged_output_path}" );
            }

            Console.WriteLine( "Creating GIF" );
            CreateCabalFamGif(combined_path);

            Console.WriteLine( "Done" );
            Thread.Sleep(3000);
        }

        private static void CreateCabalFamGif(string sourcePath)
        {
            string[] imageFiles = Directory.GetFiles(sourcePath, "*.png");
            Array.Sort(imageFiles);

            // Create the GIF
            using (var collection = new MagickImageCollection())
            {
                foreach (string file in imageFiles)
                {
                    var frame = new MagickImage(file);

                    // Optional: Resize or adjust settings
                    frame.AnimationDelay = 25; // delay in 1/100th of a second (10 = 0.1s)

                    // Optionally reduce color count for smaller file size
                    frame.Quantize(new QuantizeSettings { Colors = 256 });

                    // Add to collection
                    collection.Add(frame);

                    // Optimize frame (removes redundant pixels)
                    collection.OptimizeTransparency();
                }

                // Save the animated GIF
                string outputGifPath = Path.Combine(sourcePath, "cabal-fam.gif");
                collection.Write(outputGifPath);
                Console.WriteLine($"GIF saved to: {outputGifPath}");
            }
        }

        private static void CombineImages(string[] imagePaths, string outputPath)
        {
            var collection = new MagickImageCollection();
            MagickImage[] images = new MagickImage[imagePaths.Length];

            for( int i = 0; i < imagePaths.Length; i++)
            {
                images[i] = new MagickImage(imagePaths[i]);
                collection.Add(images[i]);
            }

            using (var combined = collection.AppendHorizontally())
            {
                combined.Write(outputPath);
            }

            foreach(var img in images)
            {
                img.Dispose();
            }
            collection.Dispose();
            images = null;
            collection = null;
        }

        private static void ExtractGif(string fileLocation, string outputDir)
        {
            using (var gifImage = System.Drawing.Image.FromFile(fileLocation))
            {
                var dimension = new FrameDimension(gifImage.FrameDimensionsList[0]);
                int frameCount = gifImage.GetFrameCount(dimension);

                for (int i = 0; i < frameCount; i++)
                {
                    gifImage.SelectActiveFrame(dimension, i);
                    Bitmap frame = new Bitmap(gifImage);
                    var outputPath = Path.Combine(outputDir, $"frame_{i:D3}.png");
                    frame.Save(outputPath, ImageFormat.Png);
                    frame.Dispose();
                }
            }
        }

        [Obsolete]
        private static string ExtractGif_v2(string fileLocation, string outputDir)
        {
            string outputPath = "";

            using (var collection = new MagickImageCollection(fileLocation))
            {
                for (int i = 0; i < collection.Count; i++)
                {
                    var frame = collection[i];
                    outputPath = Path.Combine(outputDir, $"frame_{i:D3}.png");

                    // Flatten ensures proper rendering of transparency/disposal
                    using (var flattened = new MagickImage(MagickColors.Transparent, frame.Width, frame.Height))
                    {
                        flattened.Composite(frame, CompositeOperator.Over);
                        flattened.Write(outputPath);
                    }
                }
            }

            return outputPath;
        }
    }
}
