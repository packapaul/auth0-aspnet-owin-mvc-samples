﻿using System;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;

[assembly: OwinStartup(typeof(MvcApplication.Startup))]

namespace MvcApplication
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Configure Auth0 parameters
            string auth0Domain = ConfigurationManager.AppSettings["auth0:Domain"];
            string auth0ClientId = ConfigurationManager.AppSettings["auth0:ClientId"];

            // Set Cookies as default authentication type
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = CookieAuthenticationDefaults.AuthenticationType,
                LoginPath = new PathString("/Account/Login")
            });

            // Configure OIDC middleware to work with Auth0
            var options = new OpenIdConnectAuthenticationOptions("Auth0")
            {
                Authority = $"https://{auth0Domain}",
                ClientId = auth0ClientId,
                CallbackPath = new PathString("/signin-auth0"),
                SignInAsAuthenticationType = CookieAuthenticationDefaults.AuthenticationType,
                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    RedirectToIdentityProvider = notification =>
                    {
                        if (notification.ProtocolMessage.RequestType == OpenIdConnectRequestType.AuthenticationRequest)
                        {
                            notification.ProtocolMessage.RedirectUri =
                                notification.Request.Scheme +
                                Uri.SchemeDelimiter +
                                notification.Request.Host +
                                notification.Request.PathBase +
                                notification.Options.CallbackPath;
                        }
                        else if (notification.ProtocolMessage.RequestType == OpenIdConnectRequestType.LogoutRequest)
                        {
                            var logoutUri =
                                $"https://{auth0Domain}/v2/logout?client_id={auth0ClientId}";

                            var postLogoutUri = notification.ProtocolMessage.PostLogoutRedirectUri;
                            if (!string.IsNullOrEmpty(postLogoutUri))
                            {
                                if (postLogoutUri.StartsWith("/"))
                                {
                                    // transform to absolute
                                    var request = notification.Request;
                                    postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase +
                                                    postLogoutUri;
                                }
                                logoutUri += $"&returnTo={Uri.EscapeDataString(postLogoutUri)}";
                            }

                            notification.Response.Redirect(logoutUri);
                            notification.HandleResponse();
                        }

                        return Task.FromResult(0);
                    }
                },
            };
            app.UseOpenIdConnectAuthentication(options);
        }
    }
}
