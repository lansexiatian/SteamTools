﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Application.Columns;
using System.Application.Models;
using System.Application.Security;
using System.Application.Services.CloudService.Clients;
using System.Application.Services.CloudService.Clients.Abstractions;
using System.Net.Http;
using System.Properties;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Application.Services.CloudService
{
    public abstract class CloudServiceClientBase : GeneralHttpClientFactory, ICloudServiceClient, IApiConnectionPlatformHelper
    {
        public const string ClientName_ = "CloudServiceClient";
        internal const string DefaultApiBaseUrl = ThisAssembly.Debuggable ?
            "https://steamtools-api.chinacloudsites.cn" :
            Constants.Prefix_HTTPS + "api.steampp.net";

        #region Clients

        public IAccountClient Account { get; }
        public IManageClient Manage { get; }
        public IAuthMessageClient AuthMessage { get; }
        public IVersionClient Version { get; }
        public IActiveUserClient ActiveUser { get; }
        public IAccelerateClient Accelerate { get; }

        #endregion

        readonly IApiConnection connection;
        protected readonly ICloudServiceSettings settings;
        protected readonly IAuthHelper authHelper;

        public string ApiBaseUrl { get; }

        internal ICloudServiceSettings Settings => settings;

        RSA IApiConnectionPlatformHelper.RSA
        {
            get
            {
                try
                {
                    return Settings.RSA;
                }
                catch (IsNotOfficialChannelPackageException e)
                {
                    logger.LogError(e, nameof(ApiResponseCode.IsNotOfficialChannelPackage));
                    throw new ApiResponseCodeException(ApiResponseCode.IsNotOfficialChannelPackage);
                }
            }
        }

        protected sealed override string? ClientName => ClientName_;

        public CloudServiceClientBase(
            ILogger logger,
            IHttpClientFactory clientFactory,
            IHttpPlatformHelper http_helper,
            IAuthHelper authHelper,
            IOptions<ICloudServiceSettings> options,
            IModelValidator validator) : base(logger, http_helper, clientFactory)
        {
            this.authHelper = authHelper;
            settings = options.Value;
            ApiBaseUrl = string.IsNullOrWhiteSpace(settings.ApiBaseUrl)
                ? DefaultApiBaseUrl : settings.ApiBaseUrl;
            connection = new ApiConnection(logger, this, http_helper, validator);

            #region SetClients

            Account = new AccountClient(connection);
            Manage = new ManageClient(connection);
            AuthMessage = new AuthMessageClient(connection);
            Version = new VersionClient(connection);
            ActiveUser = new ActiveUserClient(connection);
            Accelerate = new AccelerateClient(connection);

            #endregion
        }

        /// <inheritdoc cref="IHttpPlatformHelper.UserAgent"/>
        internal string UserAgent => http_helper.UserAgent;

        IAuthHelper IApiConnectionPlatformHelper.Auth => authHelper;

        public abstract Task SaveAuthTokenAsync(JWTEntity authToken);

        public abstract Task OnLoginedAsync(IReadOnlyPhoneNumber? phoneNumber, ILoginResponse response);

        public abstract void ShowResponseErrorMessage(string message);

        HttpClient IApiConnectionPlatformHelper.CreateClient() => CreateClient();

        Task<IApiResponse> ICloudServiceClient.Download(bool isAnonymous, string requestUri,
            string cacheFilePath, IProgress<float> progress, CancellationToken cancellationToken)
            => connection.DownloadAsync(cancellationToken, requestUri, cacheFilePath, progress, isAnonymous);

        Task<IApiResponse<JWTEntity>> IApiConnectionPlatformHelper.RefreshToken(JWTEntity jwt)
            => Account.RefreshToken(jwt);

        Task<HttpResponseMessage> ICloudServiceClient.Forward(
            HttpRequestMessage request,
            HttpCompletionOption completionOption,
            CancellationToken cancellationToken)
        {
            var url = request.RequestUri.ThrowIsNull(nameof(request.RequestUri)).ToString();
            url = url.Base64UrlEncode();
            request.RequestUri = new Uri($"api/forward?url={url}");
            return connection.SendAsync(request, completionOption, cancellationToken);
        }
    }
}