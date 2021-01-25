﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Security.Cryptography;
using System.Text;

namespace SSH.Stores
{
    public class OpenSSHStore : IStore
    {
        private class HashedKeysStruct
        {
            public byte[] Salt { get; set; }
            public string HostHash { get; set; }
            public string KeyName { get; set; }
            public string Fingerprint { get; set; }
        }

        private class WildcardKeysStruct
        {
            public WildcardPattern Pattern { get; set; }
            public string KeyName { get; set; }
            public string Fingerprint { get; set; }
        }

        private readonly string FileName;
        private ConcurrentDictionary<string, Tuple<string, string>> hostKeys;
        private readonly ConcurrentBag<HashedKeysStruct> hashedKeys;
        private readonly ConcurrentBag<WildcardKeysStruct> wildcardKeys;
        private bool loaded;

        public OpenSSHStore(string fileName)
        {
            FileName = fileName;
            hostKeys = new ConcurrentDictionary<string, Tuple<string, string>>();
            hashedKeys = new ConcurrentBag<HashedKeysStruct>();
            wildcardKeys = new ConcurrentBag<WildcardKeysStruct>();
        }

        public void LoadFromDisk()
        {
            if (File.Exists(FileName))
            {
                foreach (var line in File.ReadAllLines(FileName)) {
                    // skip emty lines or comments
                    // skip @cert-authority and @revoked because we do not validate
                    if (line.Length < 1 || line[0] == '#' || line[0] == '@') { continue; }

                    var hostparts = line.Split(' ');
                    // Skip invalid lines
                    if (hostparts.Length < 3 || hostparts[0].Length < 1) { continue; }
                    var (hostname, keyName, pubKey) = (hostparts[0], hostparts[1], hostparts[2]);

                    string fingerprint;
                    using (var md5 = MD5.Create())
                    {
                        var pubkey = Convert.FromBase64String(pubKey);
                        var fp_as_bytes = md5.ComputeHash(pubkey);
                        // commented out because realization below encode bytes 10,01,10 as 10:1:10 instead of classic 10:01:10
                        // so make it compatible
                        // fingerprint = System.BitConverter.ToString(fp_as_bytes).Replace('-', ':').ToLower();
                        var sb = new StringBuilder();
                        foreach (var b in fp_as_bytes)
                        {
                            sb.AppendFormat("{0:x}:", b);
                        }
                        fingerprint = sb.ToString().Remove(sb.ToString().Length - 1);
                    }

                    // hashed hostname, can be only one on line
                    if (hostname[0] == '|')
                    {
                        var hashparts = hostname.Split('|');
                        // skip invalid or unsupported lines
                        if (hashparts.Length < 4 || hashparts[1] != "1") { continue; }
                        hashedKeys.Add(
                            new HashedKeysStruct()
                            {
                                Salt = Convert.FromBase64String(hashparts[2]),
                                HostHash = hashparts[3],
                                KeyName = keyName,
                                Fingerprint = fingerprint,
                            }
                        );
                    }
                    else
                    {
                        foreach (var host in hostname.Split(','))
                        {
                            // TODO: there can be [host]:port values.
                            // We do not support it because we do not know the port
                            if (host.Length < 1 || host[0] == '[')
                            {
                                continue;
                            }
                            var (tmpHost, tmpFingerprint) = (host, fingerprint);
                            if (host[0] == '!') // Host connection denied
                            {
                                tmpHost = host.Substring(1); // clean '!'
                                tmpFingerprint = '!' + fingerprint; // make fingerprint for this host invalid
                            }
                            // wildcard pattern
                            else if (WildcardPattern.ContainsWildcardCharacters(host))
                            {
                                wildcardKeys.Add(
                                    new WildcardKeysStruct()
                                    {
                                        Pattern = new WildcardPattern(tmpHost),
                                        KeyName = keyName,
                                        Fingerprint = tmpFingerprint,
                                    }
                                );
                            }
                            // simple host
                            else
                            {
                                var hostData = new Tuple<string, string>(keyName, tmpFingerprint);
                                hostKeys.AddOrUpdate(tmpHost, hostData, (key, oldValue) => {
                                    return hostData;
                                });
                            }
                        }
                    }
                }
            }
            loaded = true;
        }

        public bool SetKey(string Host, string HostKeyName, string Fingerprint)
        {
            // It is read-only collection
            return false;
        }

        public Tuple<string, string> GetKey(string Host)
        {
            if (! loaded) { LoadFromDisk(); }
            var hostbytes = Encoding.ASCII.GetBytes(Host);
            foreach (var hashedKey in hashedKeys)
            {
                using (HMACSHA1 hmac = new HMACSHA1(hashedKey.Salt))
                {
                    var hostHash = Convert.ToBase64String(hmac.ComputeHash(hostbytes));
                    if (hostHash.Equals(hashedKey.HostHash))
                    {
                        return new Tuple<string, string>(hashedKey.KeyName, hashedKey.Fingerprint);
                    }
                }
            }
            if (hostKeys.TryGetValue(Host, out Tuple<string, string> keyData))
            {
                return keyData;
            }
            foreach (var wildcardKey in wildcardKeys)
            {
                if (wildcardKey.Pattern.IsMatch(Host))
                {
                    return new Tuple<string, string>(wildcardKey.KeyName, wildcardKey.Fingerprint);
                }
            }
            return default;
        }

    }
}
