﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AntDeployCommand.Model;
using AntDeployCommand.Utils;

namespace AntDeployCommand.Operations
{
    public class SERVICEROLL : OperationsBase
    {
        public override string ValidateArgument()
        {
            if (string.IsNullOrEmpty(Arguments.ServiceName))
            {
                return $"{Name}{nameof(Arguments.ServiceName)} required!";
            }
            if (string.IsNullOrEmpty(Arguments.DeployFolderName))
            {
                return $"{Name}{nameof(Arguments.DeployFolderName)} required!";
            }
            if (string.IsNullOrEmpty(Arguments.Host))
            {
                return $"{Name}{nameof(Arguments.Host)} required!";
            }
            if (string.IsNullOrEmpty(Arguments.Token))
            {
                return $"{Name}{nameof(Arguments.Token)} required!";
            }
            if (string.IsNullOrEmpty(Arguments.LoggerId))
            {
                Arguments.LoggerId = Guid.NewGuid().ToString("N");
            }
            return string.Empty;
        }

        public override async Task<bool> Run()
        {

            this.Info($"Host:{Arguments.Host} Start rollBack from version:" + Arguments.DeployFolderName);
            HttpRequestClient httpRequestClient = new HttpRequestClient();
            httpRequestClient.SetFieldValue("publishType", "windowservice_rollback");
            httpRequestClient.SetFieldValue("id", Arguments.LoggerId);
            httpRequestClient.SetFieldValue("serviceName", Arguments.ServiceName);
            httpRequestClient.SetFieldValue("deployFolderName", Arguments.DeployFolderName);
            httpRequestClient.SetFieldValue("Token", Arguments.Token);
            HttpLogger HttpLogger = new HttpLogger
            {
                Key = Arguments.LoggerId,
                Url = $"http://{Arguments.Host}/logger?key=" + Arguments.LoggerId
            };
            var isSuccess = true;
            WebSocketClient webSocket = new WebSocketClient(this.Log, HttpLogger);
            try
            {

                var hostKey = await webSocket.Connect($"ws://{Arguments.Host}/socket");

                httpRequestClient.SetFieldValue("wsKey", hostKey);

                var uploadResult = await httpRequestClient.Upload($"http://{Arguments.Host}/rollback", null, GetProxy());

                webSocket.ReceiveHttpAction(true);
                if (webSocket.HasError)
                {
                    isSuccess = false;
                    this.Error($"Host:{Arguments.Host},Rollback Fail,Skip to Next");
                }
                else
                {
                    if (uploadResult.Item1)
                    {
                        this.Info($"【rollback success】Host:{Arguments.Host},Response:{uploadResult.Item2}");
                    }
                    else
                    {
                        isSuccess = false;
                        this.Error($"Host:{Arguments.Host},Response:{uploadResult.Item2},Skip to Next");
                    }
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                this.Error($"Fail Rollback,Host:{Arguments.Host},Response:{ex.Message},Skip to Next");
            }
            finally
            {
                await webSocket?.Dispose();
            }
            return await Task.FromResult(isSuccess);
        }
    }
}
