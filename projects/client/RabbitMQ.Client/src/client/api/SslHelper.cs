// This source code is dual-licensed under the Apache License, version
// 2.0, and the Mozilla Public License, version 1.1.
//
// The APL v2.0:
//
//---------------------------------------------------------------------------
//   Copyright (C) 2007-2013 GoPivotal, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//---------------------------------------------------------------------------
//
// The MPL v1.1:
//
//---------------------------------------------------------------------------
//  The contents of this file are subject to the Mozilla Public License
//  Version 1.1 (the "License"); you may not use this file except in
//  compliance with the License. You may obtain a copy of the License
//  at http://www.mozilla.org/MPL/
//
//  Software distributed under the License is distributed on an "AS IS"
//  basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See
//  the License for the specific language governing rights and
//  limitations under the License.
//
//  The Original Code is RabbitMQ.
//
//  The Initial Developer of the Original Code is GoPivotal, Inc.
//  Copyright (c) 2007-2014 GoPivotal, Inc.  All rights reserved.
//---------------------------------------------------------------------------

using System;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using RabbitMQ.Client.Impl;

namespace RabbitMQ.Client
{

    ///<summary>Represents an SslHelper which does the actual heavy lifting
    ///to set up an SSL connection, using the config options in an SslOption
    ///to make things cleaner</summary>
    public class SslHelper
    {

        private SslOption m_sslOption;

        private X509Certificate CertificateSelectionCallback(object sender,
                                                             string targetHost,
                                                             X509CertificateCollection localCertificates,
                                                             X509Certificate remoteCertificate,
                                                             string[] acceptableIssuers)
        {
            if (acceptableIssuers != null && acceptableIssuers.Length > 0 &&
                localCertificates != null && localCertificates.Count > 0)
                {
                    foreach (X509Certificate certificate in localCertificates)
                        {
                            if (Array.IndexOf(acceptableIssuers, certificate.Issuer) != -1)
                                return certificate;
                        }
                }
            if (localCertificates != null && localCertificates.Count > 0)
                return localCertificates[0];

            return null;
        }

        private bool CertificateValidationCallback(object sender,
                                                   X509Certificate certificate,
                                                   X509Chain chain,
                                                   SslPolicyErrors sslPolicyErrors)
        {
            return (sslPolicyErrors & ~m_sslOption.AcceptablePolicyErrors) == SslPolicyErrors.None;
        }

        ///<summary>Upgrade a Tcp stream to an Ssl stream using the SSL options
        ///provided</summary>
        public static Stream TcpUpgrade(Stream tcpStream, SslOption sslOption, int timeout)
        {
            SslHelper helper = new SslHelper(sslOption);
            SslStream sslStream = new SslStream(tcpStream, false,
                                                new RemoteCertificateValidationCallback(helper.CertificateValidationCallback),
                                                new LocalCertificateSelectionCallback(helper.CertificateSelectionCallback));

            sslStream.AuthenticateAsClient(sslOption.ServerName,
                                           sslOption.Certs,
                                           sslOption.Version,
                                           false);
            sslStream.ReadTimeout  = timeout;
            sslStream.WriteTimeout = timeout;

            return sslStream;
        }

        private SslHelper(SslOption sslOption)
        {
            m_sslOption = sslOption;
        }

    }
}
