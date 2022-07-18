﻿using AOT;
using Nethereum.JsonRpc.Client.RpcMessages;
using Nethereum.JsonRpc.Client;
using Nethereum.JsonRpc.UnityClient;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System;
using UnityEngine;
using System.Collections;

namespace Nethereum.Unity.Metamask
{
    public class MetamaskRequestRpcClient : UnityRequest<RpcResponseMessage>, IUnityRpcRequestClient
    {

        public static ConcurrentDictionary<string, RpcResponseMessage> RequestResponses = new ConcurrentDictionary<string, RpcResponseMessage>();

        [MonoPInvokeCallback(typeof(Action<string>))]
        public static void RequestCallBack(string responseMessage)
        {
            var response = JsonConvert.DeserializeObject<RpcResponseMessage>(responseMessage);
            RequestResponses.TryAdd((string)response.Id, response);
        }

        public JsonSerializerSettings JsonSerializerSettings { get; set; }

        private string _account;

        public MetamaskRequestRpcClient(string account, JsonSerializerSettings jsonSerializerSettings = null)
        {
            if (jsonSerializerSettings == null)
                jsonSerializerSettings = DefaultJsonSerializerSettingsFactory.BuildDefaultJsonSerializerSettings();

            JsonSerializerSettings = jsonSerializerSettings;
            _account = account;
        }

        public IEnumerator SendRequest(RpcRequest request)
        {
            this.Exception = null;
            this.Result = null;
            var newUniqueRequestId = Guid.NewGuid().ToString();
            try
            {
                var metamaskRpcRequest = new MetamaskRpcRequestMessage(newUniqueRequestId, request.Method, _account,
                    request.RawParameters);

                MetamaskInterop.RequestRpcClientCallback(RequestCallBack, JsonConvert.SerializeObject(metamaskRpcRequest));

            }
            catch (Exception ex)
            {
                Exception = ex;
            }

            if (Exception == null)
            {
                var waitUntilRequestResponse = new WaitUntilRequestResponse(newUniqueRequestId);
                yield return new WaitUntil(waitUntilRequestResponse.HasReceivedResponse);


                RpcResponseMessage responseMessage = null;
                if (RequestResponses.TryRemove(newUniqueRequestId, out responseMessage))
                {
                    Result = responseMessage;
                }
                else
                {
                    Exception = new Exception("Unexpected error retrieving message");
                }

            }
        }
    }
}




