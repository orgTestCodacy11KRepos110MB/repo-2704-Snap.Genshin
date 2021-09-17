﻿using DGP.Snap.Framework.Core.Logging;
using DGP.Snap.Framework.Extensions.System.Windows.Threading;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DGP.Genshin.Models.MiHoYo
{
    public static class CookieManager
    {
        private static readonly string CookieFile = "cookie.dat";
        static CookieManager()
        {
            if (File.Exists(CookieFile))
            {
                Cookie = File.ReadAllText(CookieFile);
            }
            else
            {
                File.Create(CookieFile).Dispose();
            }
        }
        private static string Cookie { get; set; }

        public static async Task<string> GetCookieAsync()
        {
            await EnsureCookieExistAsync();
            return Cookie;
        }

        public static async Task EnsureCookieExistAsync()
        {
            if (String.IsNullOrEmpty(Cookie))
            {
                Logger.LogStatic(typeof(CookieManager), "can't find available cookie");
                await SetCookieAsync();
            }
        }

        public static async Task SetCookieAsync()
        {
            Cookie = await App.Current.Invoke(() =>
            new CookieDialog().GetInputCookieAsync());
            CookieChanged?.Invoke();
            File.WriteAllText(CookieFile, Cookie);
        }
        /// <summary>
        /// unpreventable static event.
        /// </summary>
        public static event Action CookieChanged;
    }
}