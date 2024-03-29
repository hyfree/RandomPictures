﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

using MoreNote.Common.ExtensionMethods;
using MoreNote.Common.Utils;


using MoreNote.Logic.Database;
using MoreNote.Logic.Entity;
using MoreNote.Logic.Entity.ConfigFile;

using MoreNote.Logic.Service;


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UpYunLibrary.ContentRecognition;

namespace MoreNote.Controllers
{
    public class APIController : Controller
    {
      
        private RandomImageService randomImageService;
        private DataContext dataContext;
        private ConfigFileService configFileService;

        /// <summary>
        /// 保险丝
        /// </summary>
        private readonly int _randomImageFuseSize;

        /// <summary>
        /// 保险丝计数器
        /// 当访问量查过保险丝的能力时，直接熔断接口。
        /// </summary>
        private static int _fuseCount = 0;

        private static readonly object _fuseObj = new object();

        /// <summary>
        /// 随机数组的大小
        /// </summary>
        private int size;

        /// <summary>
        /// 随即图片初始化的时间
        /// 这意味着 每小时的图片数量只有60个图片是随机选择的
        /// 每经过1小时更会一次图片
        /// </summary>
        private static int _initTime = -1;

        //目录分隔符
        private static readonly char dsc = Path.DirectorySeparatorChar;

        //private static Dictionary<string, string> typeName = new Dictionary<string, string>();
        private  WebSiteConfig webcConfig;

        private  Random random = new Random();

     
        public APIController(
             ConfigFileService configFileService,
           
            DataContext dataContext,
            RandomImageService randomImageService
            ) 
        {
           
            this.dataContext = dataContext;
            this.randomImageService = randomImageService;
            this.configFileService = configFileService;
            
            this.webcConfig = configFileService.WebConfig;
            _randomImageFuseSize = webcConfig.PublicAPI.RandomImageFuseSize;
            size = webcConfig.PublicAPI.RandomImageSize;
        }

        private class RandomImageResult
        {
            public int Error { get; set; }
            public int Result { get; set; }
            public int Count { get; set; }
            public List<string> Images { get; set; }
        }

        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> RandomImage(string type, string format = "raw", int jsonSize = 1)
        {
            var randomImageList = randomImageService.GetRandomImageList();
            lock (_fuseObj)
            {
                _fuseCount++;
            }
            RandomImage randomImage = null;
            if (DateTime.Now.Hour != _initTime)
            {
                _fuseCount = 0;
                _initTime = DateTime.Now.Hour;
            }
            else
            {
                if (_fuseCount > _randomImageFuseSize)
                {
                    Response.StatusCode = (int)HttpStatusCode.BadGateway;
                    return Content("接口遭到攻击，并发量超出限制值，触发防火墙策略。");
                }
            }

            if (string.IsNullOrEmpty(type) || !randomImageList.ContainsKey(type))
            {
                type = "动漫综合2";
            }
          
            int index = random.Next(randomImageList[type].Count - 1);
            randomImage = randomImageList[type][index];

            string ext = Path.GetExtension(randomImage.FileName);
            IHeaderDictionary headers = Request.Headers;
            StringBuilder stringBuilder = new StringBuilder();
            foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> item in headers)
            {
                stringBuilder.Append(item.Key + "---" + item.Value + "\r\n");
            }
            string RealIP = headers["X-Forwarded-For"].ToString().Split(",")[0];
            string remoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
            string remotePort = Request.HttpContext.Connection.RemotePort.ToString();
    
          
            string typeMD5 = randomImage.TypeNameMD5;

            int unixTimestamp = UnixTimeUtil.GetTimeStampInInt32();
            Console.WriteLine("现在的时间=" + unixTimestamp);
            unixTimestamp += 15;
            Console.WriteLine("过期时间=" + unixTimestamp);

            //开启token防盗链

            switch (format)
            {
                case "raw":
                    return Redirect($"{webcConfig.APPConfig.SiteUrl}/CacheServer/RandomImages/{randomImage.TypeNameMD5}/{randomImage.Id.ToHex() + ext}");

                case "json":
                    if (jsonSize < 0)
                    {
                        jsonSize = 1;
                    }

                    if (jsonSize > 20)
                    {
                        jsonSize = 20;
                    }
                    List<string> images = new List<string>();

                    for (int i = 0; i < jsonSize; i++)
                    {
                        string img = GetOneURL(type);
                        images.Add(img);
                    }

                    RandomImageResult randomImageResult = new RandomImageResult()
                    {
                        Error = 0,
                        Result = 200,
                        Count = images.Count,
                        Images = images
                    };
                    return Json(randomImageResult, Common.Utils.MyJsonConvert.GetSimpleOptions());

                default:
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Content("format=??");
            }
        }

        private string GetOneURL(string type)
        {
            var randomImageList = randomImageService.GetRandomImageList();
            RandomImage randomImage = null;
            int index = random.Next(randomImageList[type].Count - 1);
            randomImage = randomImageList[type][index];

            string ext = Path.GetExtension(randomImage.FileName);
          

            return $"{webcConfig.APPConfig.SiteUrl}/CacheServer/RandomImages/{randomImage.TypeNameMD5}/{randomImage.Id.ToHex() + ext}";
        }

      

        public IActionResult GetRandomImageFuseSize()
        {
            return Content(_fuseCount.ToString());
        }

        [HttpPost]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> UpYunImageServiceHook()
        {
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                try
                {
                    string body = await reader.ReadToEndAsync().ConfigureAwait(true);
                    ContentIdentifiesHookMessages message = JsonSerializer.Deserialize<ContentIdentifiesHookMessages>(body, Common.Utils.MyJsonConvert.GetLeanoteOptions());
                    if (string.IsNullOrEmpty(message.uri) || message.type == UpyunType.test)
                    {
                        Response.StatusCode = 200;
                        return Content("未找到");
                    }
                    string fileSHA1 = Path.GetFileNameWithoutExtension(message.uri);

                    RandomImage imagedb = dataContext.RandomImage.Where(b => b.FileSHA1.Equals(fileSHA1)).FirstOrDefault();
                    if (imagedb == null)
                    {
                        Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return Content("未找到");
                    }
                    switch (message.type)
                    {
                        case UpyunType.delete:
                            imagedb.IsDelete = true;
                            break;

                        case UpyunType.shield:
                            imagedb.Block = true;
                            break;

                        case UpyunType.cancel_shield:
                            imagedb.Block = false;
                            break;

                        case UpyunType.forbidden:
                            imagedb.Block = true;
                            break;

                        case UpyunType.cancel_forbidden:
                            imagedb.Block = false;
                            break;

                        default:
                            break;
                    }
                    dataContext.SaveChanges();
                    // Do something
                }
                catch (Exception ex)
                {
                    Response.StatusCode = 404;
                    return Content("false" + ex.Message);
                }
            }
            Response.StatusCode = 200;
            return Content("ok");
        }

 
       
    }
}