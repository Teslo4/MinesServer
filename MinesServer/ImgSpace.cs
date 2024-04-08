using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Security.Policy;
using MinesServer.Server;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing;
using Azure;
using System.Net.Mime;
using System.Drawing.Imaging;
using System.Net.Http.Headers;

namespace MinesServer
{
    public class ImgSpace
    {
        public ImgSpace() { _serverspace.Start(); }
        static HttpListener _serverspace = new HttpListener();
        public static string UrlB(string urlend) => $"http://localhost/{urlend}/";
        public static string LocateChunks(string urlend,(int x, int y)start,(int x, int y) end)
        {
            var url = UrlB(urlend);
            LocateArray(url, M3Compressor.splitbitmap(Default.ConvertMapPart(start.x * 32, start.y * 32, end.x * 32, end.y * 32)));
            return url;
        }
        public static string LocateImg(string urlend,string url)
        {
            var result = UrlB(urlend);
            LocateArray(result,M3Compressor.CompressLarge(Load(url)));
            return result;
        }
        private static Bitmap Load(string url)
        {
            var client = new HttpClient();
            var x = client.GetStreamAsync(url).Result;
            return  (Bitmap)resizeImage(200,200,Image.FromStream(x));
        }
        public static Image resizeImage(int newWidth, int newHeight, Image imgPhoto)
        {

            int sourceWidth = imgPhoto.Width;
            int sourceHeight = imgPhoto.Height;

            //Consider vertical pics
            if (sourceWidth < sourceHeight)
            {
                int buff = newWidth;

                newWidth = newHeight;
                newHeight = buff;
            }

            int sourceX = 0, sourceY = 0, destX = 0, destY = 0;
            float nPercent = 0, nPercentW = 0, nPercentH = 0;

            nPercentW = ((float)newWidth / (float)sourceWidth);
            nPercentH = ((float)newHeight / (float)sourceHeight);
            if (nPercentH < nPercentW)
            {
                nPercent = nPercentH;
                destX = System.Convert.ToInt16((newWidth -
                          (sourceWidth * nPercent)) / 2);
            }
            else
            {
                nPercent = nPercentW;
                destY = System.Convert.ToInt16((newHeight -
                          (sourceHeight * nPercent)) / 2);
            }

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);


            Bitmap bmPhoto = new Bitmap(newWidth, newHeight,
                          PixelFormat.Format24bppRgb);

            bmPhoto.SetResolution(imgPhoto.HorizontalResolution,
                         imgPhoto.VerticalResolution);

            Graphics grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.Clear(Color.Black);
            grPhoto.InterpolationMode =
                System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            grPhoto.DrawImage(imgPhoto,
                new Rectangle(destX, destY, destWidth, destHeight),
                new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                GraphicsUnit.Pixel);

            grPhoto.Dispose();
            imgPhoto.Dispose();
            return bmPhoto;
        }
        private static byte[] GetArrayFromChunks((int x,int y)start,(int x,int y)end) => M3Compressor.Compress(Default.ConvertMapPart(start.x * 32, start.y * 32, end.x * 32, end.y * 32));
        public static void LocateArray(string url, byte[] array)
        {
                if (_serverspace.Prefixes.Contains(url))
                {
                    return;
                }
                _serverspace.Prefixes.Add(url);
                var startime = ServerTime.Now;
                Task.Run(() =>
                {
                    while (ServerTime.Now - startime < TimeSpan.FromSeconds(10))
                    {
                        var context = _serverspace.GetContextAsync().ContinueWith((a) =>
                        {
                            try
                            {
                                var context = a.Result;
                                context.Response.ContentLength64 = array.Length;
                                context.Response.OutputStream.Write(array, 0, array.Length);
                                Thread.Sleep(500);
                                context.Response.Close();
                                _serverspace.Prefixes.Remove(url);
                            }
                            catch (Exception ex)
                            {
                                if (_serverspace.Prefixes.Contains(url))
                                {
                                    _serverspace.Prefixes.Remove(url);
                                }
                                Console.WriteLine(ex);
                            }

                        });
                        Thread.Sleep(1);
                    }
                    if (_serverspace.Prefixes.Contains(url))
                    {
                        _serverspace.Prefixes.Remove(url);
                    }
                });
        }
    }
}
