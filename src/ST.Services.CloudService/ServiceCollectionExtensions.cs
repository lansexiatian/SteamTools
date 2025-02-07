﻿using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Application.Services;
using System.Application.Services.CloudService;
using System.Properties;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 尝试添加 CloudServiceClient
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="useMock"></param>
        /// <returns></returns>
        public static IServiceCollection TryAddCloudServiceClient<T>(this IServiceCollection services, bool useMock = false)
            where T : CloudServiceClientBase
        {
            services.TryAddHttpPlatformHelper();
            if (useMock && ThisAssembly.Debuggable)
            {
#if DEBUG
                services.AddSingleton<ICloudServiceClient, MockCloudServiceClient>();
#else
                throw new NotSupportedException();
#endif
            }
            else
            {
                services.AddHttpClient(CloudServiceClientBase.ClientName_, (s, c) =>
                {
                    var sc = s.GetRequiredService<CloudServiceClientBase>();
                    c.Timeout = ThisAssembly.Debuggable ? TimeSpan.FromSeconds(29) : TimeSpan.FromSeconds(19); // 19s
                    c.BaseAddress = new Uri(sc.ApiBaseUrl);
                    c.DefaultRequestHeaders.UserAgent.ParseAdd(sc.UserAgent);
                    c.DefaultRequestHeaders.Add(Constants.Headers.Request.AppVersion, sc.Settings.AppVersion.ToStringN());
#if NET5_0_OR_GREATER
                    c.DefaultRequestVersion = System.Net.HttpVersion.Version20;
#endif
                });
                services.TryAddSingleton<T>();
                services.TryAddSingleton<ICloudServiceClient>(s => s.GetRequiredService<T>());
                services.TryAddSingleton<CloudServiceClientBase>(s => s.GetRequiredService<T>());
            }
            return services;
        }
    }
}