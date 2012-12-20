/*
Copyright 2012 Xamarin Inc

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

	http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Linq;
using System.Net;
using Xamarin.Auth;

namespace Google.Apis.Authentication.OAuth2
{
	public class GoogleAuthenticator
		: OAuth2Authenticator, IAuthenticator
	{
		public GoogleAuthenticator (string clientId, Uri callbackUri, params string[] scopes)
			: base (
				clientId,
				(scopes ?? Enumerable.Empty<string>()).Aggregate (String.Empty, (o,s) => o + " " + s),
				new Uri ("https://accounts.google.com/o/oauth2/auth"),
				callbackUri,
				null)
		{
			Completed += (sender, args) => { Account = args.Account; };
		}

		public Account Account
		{
			get;
			set;
		}

		public void ApplyAuthenticationToRequest (HttpWebRequest request)
		{
			if (Account == null)
				throw new InvalidOperationException ("You must be authenticated to make requests");

			string token = Account.Properties["access_token"];
			string type = Account.Properties["token_type"];
			request.Headers[HttpRequestHeader.Authorization] = String.Format ("{0} {1}", type, token);
		}
	}
}