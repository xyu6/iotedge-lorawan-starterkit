﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.NetworkServer
{
    using System;
    using System.Globalization;
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// LoRa payload decoder
    /// </summary>
    public class LoRaPayloadDecoder : ILoRaPayloadDecoder
    {
        // Http client used by decoders
        // Decoder calls don't need proxy since they will never leave the IoT Edge device
        Lazy<HttpClient> decodersHttpClient = new Lazy<HttpClient>(() =>
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            client.DefaultRequestHeaders.Add("Keep-Alive", "timeout=86400");
            return client;
        });

        public async ValueTask<JObject> DecodeMessageAsync(byte[] payload, byte fport, string sensorDecoder)
        {
            sensorDecoder = sensorDecoder ?? string.Empty;

            string result;
            var base64Payload = Convert.ToBase64String(payload);

            // Call local decoder (no "http://" in SensorDecoder)
            if (!sensorDecoder.Contains("http://"))
            {
                Type decoderType = typeof(LoRaPayloadDecoder);
                MethodInfo toInvoke = decoderType.GetMethod(sensorDecoder, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);

                if (toInvoke != null)
                {
                    result = (string)toInvoke.Invoke(null, new object[] { payload, fport });
                }
                else
                {
                    result = $"{{\"error\": \"No '{sensorDecoder}' decoder found\", \"rawpayload\": \"{base64Payload}\"}}";
                }
            }

            // Call SensorDecoderModule hosted in seperate container ("http://" in SensorDecoder)
            // Format: http://containername/api/decodername
            else
            {
                string toCall = sensorDecoder;

                if (sensorDecoder.EndsWith("/"))
                {
                    toCall = sensorDecoder.Substring(0, sensorDecoder.Length - 1);
                }

                // use HttpUtility to UrlEncode Fport and payload
                string fportEncoded = HttpUtility.UrlEncode(fport.ToString());
                string payloadEncoded = HttpUtility.UrlEncode(base64Payload);

                // Add Fport and Payload to URL
                toCall = $"{toCall}?fport={fportEncoded}&payload={payloadEncoded}";

                // Call SensorDecoderModule
                result = await this.CallSensorDecoderModule(toCall, payload);
            }

            JObject resultJson;

            // Verify that result is valid JSON.
            try
            {
                resultJson = JObject.Parse(result);
            }
            catch
            {
                resultJson = JObject.Parse($"{{\"error\": \"Invalid JSON returned from '{sensorDecoder}'\", \"rawpayload\": \"{base64Payload}\"}}");
            }

            return resultJson;
        }

        async Task<string> CallSensorDecoderModule(string sensorDecoderModuleUrl, byte[] payload)
        {
            var base64Payload = Convert.ToBase64String(payload);
            string result = string.Empty;

            try
            {
                HttpResponseMessage response = await this.decodersHttpClient.Value.GetAsync(sensorDecoderModuleUrl);

                if (!response.IsSuccessStatusCode)
                {
                    var badReqResult = await response.Content.ReadAsStringAsync();

                    result = JsonConvert.SerializeObject(new
                    {
                        error = $"SensorDecoderModule '{sensorDecoderModuleUrl}' returned bad request.",
                        exceptionMessage = badReqResult ?? string.Empty,
                        rawpayload = base64Payload
                    });
                }
                else
                {
                    result = await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error in decoder handling: {ex.Message}", LogLevel.Error);

                result = JsonConvert.SerializeObject(new
                {
                    error = $"Call to SensorDecoderModule '{sensorDecoderModuleUrl}' failed.",
                    exceptionMessage = ex.Message,
                    rawpayload = base64Payload
                });
            }

            return result;
        }

        /// <summary>
        /// Value sensor decoding, from <see cref="byte[]"/> to <see cref="string"/>
        /// </summary>
        /// <param name="payload">The payload to decode</param>
        /// <param name="fport">The received frame port</param>
        /// <returns>The decoded value as a JSON string</returns>
        public static string DecoderValueSensor(byte[] payload, uint fport)
        {
            var result = Encoding.UTF8.GetString(payload);

            var isSupportedNumericValue = double.TryParse(result, NumberStyles.Float, CultureInfo.InvariantCulture, out double ignored);
            var quotes = isSupportedNumericValue ? string.Empty : "\"";

            return $"{{ \"value\":{quotes}{result}{quotes} }}";
        }
    }
}