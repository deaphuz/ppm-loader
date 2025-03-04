using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace GK_Projekt2
{
    public class PPMImage
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int MaxColorValue { get; private set; }
        public byte[] Pixels { get; private set; }

        public Bitmap ReadP3(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Nie znaleziono pliku.");
            }

            using (var reader = new StreamReader(filePath, System.Text.Encoding.ASCII, false, 8192))
            {
                // Read PPM header
                string line = reader.ReadLine()?.Trim();
                if (line != "P3")
                {
                    throw new FormatException("Nieprawidłowy format PPM.");
                }

                int width = 0, height = 0, maxColorValue = 255;

                bool widthFound = false, heightFound = false;
                do
                {
                    line = line.Trim();
                    if (line.StartsWith("#") || line == string.Empty) continue; 

                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        int value;
                        if (int.TryParse(part, out value))
                        {
                            if (!widthFound)
                            {
                                width = value;
                                widthFound = true;
                            }
                            else if (!heightFound)
                            {
                                height = value;
                                heightFound = true;
                            }
                        }
                        if (widthFound && heightFound) break;
                    }
                } while ((!widthFound || !heightFound) && ((line = reader.ReadLine()) != null));

                if (!widthFound || !heightFound)
                {
                    throw new FormatException("Nieprawidłowe rozmiary w pliku PPM.");
                }

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("#") || line == string.Empty) continue;
                    maxColorValue = int.Parse(line);
                    break;
                }

                bool needsScaling = maxColorValue > 255;
                Bitmap bitmap = new Bitmap(width, height);

                //odczytywanie pixeli
                int x = 0, y = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("#") || line == string.Empty) continue;//pomijanie komentarzy

                    var colors = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < colors.Length; i += 3)
                    {
                        if (i + 2 >= colors.Length) break;

                        int r = int.Parse(colors[i]);
                        int g = int.Parse(colors[i + 1]);
                        int b = int.Parse(colors[i + 2]);

                        //skalowanie
                        if (needsScaling)
                        {
                            r = (int)(r / (float)maxColorValue * 255);
                            g = (int)(g / (float)maxColorValue * 255);
                            b = (int)(b / (float)maxColorValue * 255);
                        }
                        r = Clamp(r, 0, 255);
                        g = Clamp(g, 0, 255);
                        b = Clamp(b, 0, 255);
                        bitmap.SetPixel(x, y, Color.FromArgb(r, g, b));

                        if (++x >= width)
                        {
                            x = 0;
                            if (++y >= height) break;
                        }
                    }
                    if (y >= height) break;
                }
                return bitmap;
            }
        }

        public Bitmap ReadP6(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Nie znaleziono pliku.");
            }

            using (var reader = new BinaryReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192)))
            {
                string format = ReadLine(reader);
                if (format != "P6")
                {
                    throw new FormatException("Nieprawidłowy format PPM");
                }

                int width = 0, height = 0, maxColorValue = 255;

                try
                {
                    string line;
                    while ((line = ReadLine(reader)) != null)
                    {
                        line = line.Trim();
                        if (line.StartsWith("#") || line == string.Empty) continue;

                        var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2 && width == 0 && height == 0)
                        {
                            width = int.Parse(parts[0]);
                            height = int.Parse(parts[1]);
                        }
                        else if (parts.Length == 1 && maxColorValue == 255)
                        {
                            maxColorValue = int.Parse(parts[0]);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas odczytu pliku: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }


                if (width == 0 || height == 0)
                {
                    throw new FormatException("Nieprawidłowe wymiary pliku PPM");
                }

                bool needsScaling = maxColorValue > 255 && maxColorValue <= 65535;
                Bitmap bitmap = new Bitmap(width, height);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int r, g, b;
                        if (needsScaling)
                        {
                            r = (reader.ReadByte() << 8) | reader.ReadByte();
                            g = (reader.ReadByte() << 8) | reader.ReadByte();
                            b = (reader.ReadByte() << 8) | reader.ReadByte();

                            r = (int)(r / (float)maxColorValue * 255);
                            g = (int)(g / (float)maxColorValue * 255);
                            b = (int)(b / (float)maxColorValue * 255);
                        }
                        else
                        {
                            r = reader.ReadByte();
                            g = reader.ReadByte();
                            b = reader.ReadByte();
                        }

                        r = Clamp(r, 0, 255);
                        g = Clamp(g, 0, 255);
                        b = Clamp(b, 0, 255);

                        Color color = Color.FromArgb(r, g, b);
                        bitmap.SetPixel(x, y, color);
                    }
                }

                return bitmap;
            }
        }

        private string ReadLine(BinaryReader reader)
        {
            string result = "";
            char ch;
            try
            {
                while ((ch = reader.ReadChar()) != '\n')
                {
                    result += ch;
                }
            } catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas odczytu pliku: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return result;
        }

        private void ReadDimensionsAndMaxValue(StreamReader reader)
        {
            string line;

            // Czytanie linii dopóki nie znajdziemy wymiarów
            do
            {
                line = reader.ReadLine()?.Trim();
            }
            while (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"));

            // Sprawdzenie czy linia zawiera dane o wymiarach
            string[] dims = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (dims.Length < 2)
                throw new InvalidDataException("Brak wystarczających danych wymiarów w pliku PPM.");

            // Parsowanie szerokości i wysokości
            if (!int.TryParse(dims[0], out int width))
                throw new InvalidDataException("Nieprawidłowa wartość szerokości w danych PPM.");
            if (!int.TryParse(dims[1], out int height))
                throw new InvalidDataException("Nieprawidłowa wartość wysokości w danych PPM.");

            Width = width;
            Height = height;

            // Czytanie linii dopóki nie znajdziemy maksymalnej wartości koloru
            do
            {
                line = reader.ReadLine()?.Trim();
            }
            while (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"));

            if (!int.TryParse(line, out int maxColorValue))
                throw new InvalidDataException("Nieprawidłowa wartość maksymalna koloru w danych PPM.");

            MaxColorValue = maxColorValue;
        }

        public static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }
    }
}
