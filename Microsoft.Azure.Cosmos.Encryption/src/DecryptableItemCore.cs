﻿// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Encryption
{
    using System;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    internal sealed class DecryptableItemCore : DecryptableItem
    {
        /// <summary>
        /// The encrypted content which is yet to be decrypted.
        /// </summary>
        private readonly JToken decryptableContent;
        private readonly Encryptor encryptor;
        private readonly CosmosSerializer cosmosSerializer;

        internal EncryptionProcessor EncryptionProcessor { get; }

        public DecryptableItemCore(
            JToken decryptableContent,
            Encryptor encryptor,
            EncryptionProcessor encryptionProcessor,
            CosmosSerializer cosmosSerializer)
        {
            this.decryptableContent = decryptableContent ?? throw new ArgumentNullException(nameof(decryptableContent));
            this.encryptor = encryptor ?? throw new ArgumentNullException(nameof(encryptor));
            this.EncryptionProcessor = encryptionProcessor ?? throw new ArgumentNullException(nameof(encryptionProcessor));
            this.cosmosSerializer = cosmosSerializer ?? throw new ArgumentNullException(nameof(cosmosSerializer));
        }

        public override async Task<(T, DecryptionContext)> GetItemAsync<T>()
        {
            if (!(this.decryptableContent is JObject document))
            {
                return (this.cosmosSerializer.FromStream<T>(EncryptionProcessor.BaseSerializer.ToStream(this.decryptableContent)), null);
            }

            try
            {
                (JObject decryptedItem, DecryptionContext decryptionContext) = await this.EncryptionProcessor.DecryptAsync(
                    document,
                    this.encryptor,
                    cancellationToken: default);

                return (this.cosmosSerializer.FromStream<T>(EncryptionProcessor.BaseSerializer.ToStream(decryptedItem)), decryptionContext);
            }
            catch (Exception exception)
            {
                string dataEncryptionKeyId = !(document.TryGetValue(Constants.EncryptedInfo, out JToken encryptedInfo) &&
                    (encryptedInfo is JObject encryptedInfoObject))
                    ? null
                    : (string)encryptedInfoObject.GetValue(Constants.EncryptionDekId);

                throw new EncryptionException(
                    dataEncryptionKeyId,
                    this.decryptableContent.ToString(),
                    exception);
            }
        }
    }
}
