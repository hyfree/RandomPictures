using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MoreNote.Logic.Database;
using MoreNote.Logic.Entity;
using MoreNote.Logic.Entity.ConfigFile;
using MoreNote.Logic.Service;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MoreNoteWorkerService
{
    /// <summary>
    /// 随机图片接口
    /// </summary>
    public class UpdataImageURLWorker : BackgroundService
    {
       
        private RandomImageService randomImageService;

        /// <summary>
        /// 随机图片列表
        /// </summary>

        /// <summary>
        /// 网站配置
        /// </summary>
        private readonly WebSiteConfig config;

        /// <summary>
        /// 每个系列的随机图片数量
        /// </summary>
        private readonly int _randomImageSize;

       

     
        private readonly Random random = new Random();

        public UpdataImageURLWorker(DataContext dataContext,ConfigFileService configFileService )
        {
           
            this.randomImageService = new RandomImageService(dataContext);
          
            config = configFileService.WebConfig;
            _randomImageSize = config.PublicAPI.RandomImageSize;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int delaySecondTime = config.PublicAPI.UpdateTime;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdatImage().ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(delaySecondTime), stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    
                    await Task.Delay(TimeSpan.FromSeconds(delaySecondTime), stoppingToken).ConfigureAwait(false);
                }
            }
        }

        private async Task UpdatImage()
        {
            var imageTypeList = randomImageService.GetImageTypeList();
            var randomImageList = randomImageService.GetRandomImageList();

            for (int y = 0; y < imageTypeList.Count; y++)
            {
                if (!randomImageList.ContainsKey(imageTypeList[y]))
                {
                    randomImageList.Add(imageTypeList[y], new List<RandomImage>(_randomImageSize));
                }

                if (randomImageList[imageTypeList[y]].Count >= _randomImageSize)
                {
                    RandomImage randomImage = randomImageService.GetRandomImage(imageTypeList[y]);
                    randomImageList[imageTypeList[y]][random.Next(0, randomImageList.Count)] = randomImage;
                }
                else
                {
                    RandomImage randomImage = randomImageService.GetRandomImage(imageTypeList[y]);
                    randomImageList[imageTypeList[y]].Add(randomImage);
                }
            }
        }
    }
}