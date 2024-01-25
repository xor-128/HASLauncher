using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HASLibrary
{
    public static class DownloadManager
    {
        private static readonly HttpClient _httpClient = new();

        public static string GetFileAsString(string url)
        {
            var result = Task.Run(() => GetFileAsStringAsync(url)).Result;
            return result;
        }
        public static async Task<string> GetFileAsStringAsync(string url)
        {
            var request = await _httpClient.GetAsync(url);
            var response = await request.Content.ReadAsStringAsync();
            return response;
        }
        
        public static byte[] GetFileAsBytes(string url)
        {
            var result = Task.Run(() => GetFileAsBytesAsync(url)).Result;
            return result;
        }
        public static async Task<byte[]> GetFileAsBytesAsync(string url)
        {
            var request = await _httpClient.GetAsync(url);
            var response = await request.Content.ReadAsByteArrayAsync();
            return response;
        }

        internal static bool DownloadFile(string url, string path)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                var request = GetFileAsBytes(url);

                File.WriteAllBytes(path, request);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
            
            return true;
        }
    }
}
