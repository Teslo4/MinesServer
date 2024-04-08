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
                    while (ServerTime.Now - startime < TimeSpan.FromSeconds(5))
                    {
                        var context = _serverspace.GetContextAsync().ContinueWith((a) =>
                        {
                            try
                            {
                                var context = a.Result;
                                byte[] _responseArray = array;
                                context.Request.Headers.Add("Content-Disposition", "attachment; filename=mappart.m3");
                                context.Response.OutputStream.WriteAsync(_responseArray, 0, _responseArray.Length);
                                context.Response.KeepAlive = false;
                                context.Response.Close();
                                _serverspace.Prefixes.Remove(url);
                            }
                            catch (Exception ex)
                            {
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
