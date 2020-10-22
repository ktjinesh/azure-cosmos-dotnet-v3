﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Encryption
{
    using System;
    using Microsoft.Data.Encryption.Cryptography;

    /// <summary>
    /// Encryption Algorithm provided by MDE Encryption Package
    /// </summary>
    internal sealed class MdeEncryptionAlgorithm : DataEncryptionKey
    {
        private readonly EncryptionAlgorithm mdeEncryptionAlgorithm;

        // unused for MDE Algorithm.
        public override byte[] RawKey => null;

        public override string EncryptionAlgorithm => CosmosEncryptionAlgorithm.MdeAEAes256CbcHmacSha256Randomized;

        /// <summary>
        /// Initializes a new instance of MdeEncryptionAlgorithm.
        /// </summary>
        /// <param name="dekProperties"> Data Encryption Key properties</param>
        /// <param name="rawDek"> Raw bytes of Data Encryption Key </param>
        /// <param name="encryptionType"> Encryption type </param>
        /// <param name="encryptionKeyStoreProvider"> EncryptionKeyStoreProvider for wrapping and unwrapping </param>
        public MdeEncryptionAlgorithm(
            DataEncryptionKeyProperties dekProperties,
            Data.Encryption.Cryptography.EncryptionType encryptionType,
            EncryptionKeyStoreProvider encryptionKeyStoreProvider,
            TimeSpan? cacheTimeToLive)
        {
            if (dekProperties == null)
            {
                throw new ArgumentNullException(nameof(dekProperties));
            }

            if (encryptionKeyStoreProvider == null)
            {
                throw new ArgumentNullException(nameof(encryptionKeyStoreProvider));
            }

            string keyName = dekProperties.EncryptionKeyWrapMetadata.GetName(dekProperties.EncryptionKeyWrapMetadata);

            KeyEncryptionKey keyEncryptionKey = KeyEncryptionKey.GetOrCreate(
                keyName,
                dekProperties.EncryptionKeyWrapMetadata.Value,
                encryptionKeyStoreProvider);

            ProtectedDataEncryptionKey protectedDataEncryptionKey;
            if (cacheTimeToLive.HasValue)
            {
                // no caching
                if (cacheTimeToLive.Value == TimeSpan.FromMilliseconds(0))
                {
                    protectedDataEncryptionKey = new ProtectedDataEncryptionKey(
                        dekProperties.Id,
                        keyEncryptionKey,
                        dekProperties.WrappedDataEncryptionKey);
                }
                else
                {
                    protectedDataEncryptionKey = ProtectedDataEncryptionKey.GetOrCreate(
                       dekProperties.Id,
                       keyEncryptionKey,
                       dekProperties.WrappedDataEncryptionKey);

                    protectedDataEncryptionKey.TimeToLive = cacheTimeToLive.Value;
                }
            }
            else
            {
                protectedDataEncryptionKey = ProtectedDataEncryptionKey.GetOrCreate(
                       dekProperties.Id,
                       keyEncryptionKey,
                       dekProperties.WrappedDataEncryptionKey);
            }

            this.mdeEncryptionAlgorithm = Data.Encryption.Cryptography.EncryptionAlgorithm.GetOrCreate(
                protectedDataEncryptionKey,
                encryptionType);
        }

        /// <summary>
        /// Encrypt data using EncryptionAlgorithm
        /// </summary>
        /// <param name="plainText">Plaintext data to be encrypted</param>
        /// <returns>Returns the ciphertext corresponding to the plaintext.</returns>
        public override byte[] EncryptData(byte[] plainText)
        {
            return this.mdeEncryptionAlgorithm.Encrypt(plainText);
        }

        /// <summary>
        /// Decrypt data using EncryptionAlgorithm
        /// </summary>
        /// <param name="cipherText">CipherText data to be decrypted</param>
        /// <returns>Returns the plaintext corresponding to the cipherText.</returns>
        public override byte[] DecryptData(byte[] cipherText)
        {
            return this.mdeEncryptionAlgorithm.Decrypt(cipherText);
        }
    }
}