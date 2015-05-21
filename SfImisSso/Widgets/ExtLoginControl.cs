using ServiceStack.Text;
using SfImisSso.Configs;
using SfImisSso.Models;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Web.UI.WebControls;
using Telerik.Sitefinity.Configuration;
using Telerik.Sitefinity.Data;
using Telerik.Sitefinity.Model;
using Telerik.Sitefinity.Security;
using Telerik.Sitefinity.Security.Configuration;
using Telerik.Sitefinity.Security.Data;
using Telerik.Sitefinity.Security.Model;
using Telerik.Sitefinity.Security.Web.UI;
using Telerik.Sitefinity.Services;
using Telerik.Sitefinity.Web.UI.PublicControls;
namespace SfImisSso.Widgets
{
	public class ExtLoginControl : LoginControl
	{
		private User currentUser;
		protected override void CreateChildControls()
		{
			base.CreateChildControls();
			this.UserNameLabel.Text = "Email address";
			this.UserNameRequiredLiteral.Text = "Enter your email address";
		}

		protected override void OnLoggedIn(EventArgs e)
		{
			if(this.Page != null)
			{
				if (this.LoginAction == SuccessfulLoginAction.Redirect) 
				{
					string redirectUrl = this.Page.Request.QueryString["ReturnUrl"];
					if (string.IsNullOrEmpty(redirectUrl))
						redirectUrl = this.DestinationPageUrl;
					else
					{
						// RequestUrl parameter exists, use it.
						// Check if redirecting to moc.surgonc.org, vm, or sosap
						var domainUrls = Config.Get<ImisConfig>().ConnectUrls;
						var domains = domainUrls.Elements.Select(i => i.Url);
						if (domains.Any(redirectUrl.Contains))
						{
							if (HttpContext.Current.Response.Cookies["iMIS"] != null)
							{
								var cookie = HttpContext.Current.Response.Cookies["iMIS"];
								string imisId = cookie.Value;
								if (!imisId.IsNullOrWhitespace())
								{
									// Call web service to get ContactGuid
									// Attach to redirect url and complete redirect
									var contactResponse = AttemptContactRequest(imisId);
									if (contactResponse.ContactGuid != null & contactResponse.ContactGuid != Guid.Empty)
									{
										redirectUrl = redirectUrl + "?ContactGuid=" + contactResponse.ContactGuid.ToString();
									}
								}
							}
						}

						redirectUrl = HttpUtility.UrlDecode(redirectUrl);
					}

					this.Page.Response.Redirect(redirectUrl, true);
				}
				else // close window
				{
					var url = Config.Get<SecurityConfig>().Permissions[SecurityConstants.Sets.Backend.SetName].AjaxLoginUrl;
					if (url.Contains('?'))
						url += "&closeWindow=true";
					else
						url += "?closeWindow=true";
					this.DestinationPageUrl = url;
				}
			}
		}

		protected override void LoginForm_Authenticate(object sender, AuthenticateEventArgs e)
		{
			if (String.IsNullOrEmpty(this.MembershipProvider))
				this.MembershipProvider = UserManager.GetDefaultProviderName();

			UserLoggingReason result;

			var imisResponse = AttemptIMISLogin(e);
			if (imisResponse != null && imisResponse.IsAuthenticated == true)
			{
				var user = GetOrCreateSitefinityUser(imisResponse);

				//Create Cookie for iMIS
				HttpCookie imisCookie = new HttpCookie("iMIS", imisResponse.ImisId) { Domain = Config.Get<ImisConfig>().DomainUrl };
				imisCookie.Expires = DateTime.Now.AddDays(1d);
				HttpContext.Current.Response.Cookies.Add(imisCookie);

				result = SecurityManager.AuthenticateUser(this.MembershipProvider, user.UserName, this.RememberMeSet, out this.currentUser);
			}
			else
			{
				result = SecurityManager.AuthenticateUser(this.MembershipProvider, this.UserName, this.Password, this.RememberMeSet, out this.currentUser);
			}

			e.Authenticated = result == UserLoggingReason.Success;
			if (!e.Authenticated)
			{
				PrepareMessage(result);
				var context = SystemManager.CurrentHttpContext;
				var user = (SitefinityPrincipal)context.User;
				if (result == UserLoggingReason.UserLimitReached
					|| result == UserLoggingReason.UserLoggedFromDifferentIp
					|| result == UserLoggingReason.UserLoggedFromDifferentComputer
					|| result == UserLoggingReason.UserAlreadyLoggedIn)
				{
					PrepareWorkflowPanels(user.IsUnrestricted, result);
				}
			}
		}

		protected ImisResponse AttemptIMISLogin(AuthenticateEventArgs e)
		{
			// Get request url from configs
			var requestUrl = Config.Get<ImisConfig>().AuthUrl;
			if (String.IsNullOrWhiteSpace(requestUrl))
				return null;

			// Set data for request response
			NameValueCollection data = new NameValueCollection() { { "username", this.UserName }, { "password", this.Password } };
			ImisResponse response = null;

			//send and recieve data
			using (WebClient client = new WebClient())
			{
				var responseBytes = client.UploadValues(requestUrl, "POST", data);
				response = JsonSerializer.DeserializeFromString<ImisResponse>(Encoding.UTF8.GetString(responseBytes));
			}

			return response;
		}

		protected ContactResponse AttemptContactRequest(string imisId)
		{
			// Get request url from configs
			var requestUrl = Config.Get<ImisConfig>().ContactUrl;
			if (String.IsNullOrWhiteSpace(requestUrl))
				return null;

			// Set data for request response
			NameValueCollection data = new NameValueCollection() { { "iMISID", imisId } };
			ContactResponse response = null;

			//send and recieve data
			using (WebClient client = new WebClient())
			{
				var responseBytes = client.UploadValues(requestUrl, "POST", data);
				response = JsonSerializer.DeserializeFromString<ContactResponse>(Encoding.UTF8.GetString(responseBytes));
			}

			return response;
		}

		protected User GetOrCreateSitefinityUser(ImisResponse data)
		{

			UserManager uMan = UserManager.GetManager();
			uMan.Provider.SuppressSecurityChecks = true;
			UserProfileManager upMan = UserProfileManager.GetManager();
			upMan.Provider.SuppressSecurityChecks = true;

			var sfUser = uMan.GetUser(this.UserName);

			if (sfUser == null)
			{

				var randomPassword = Membership.GeneratePassword(8, 0);
				MembershipCreateStatus status;
				sfUser = uMan.CreateUser(this.UserName, randomPassword, data.Email, "", "", true, null, out status);
				if (status == MembershipCreateStatus.Success)
				{
					SitefinityProfile sfProfile = upMan.CreateProfile(sfUser, Guid.NewGuid(), typeof(SitefinityProfile)) as SitefinityProfile;
					if (sfProfile != null)
					{
						sfProfile.FirstName = data.FirstName;
						sfProfile.LastName = data.LastName;
						sfProfile.SetValue("ImisId", data.ImisId);
						sfProfile.SetValue("MiddleName", data.MiddleName);
						sfProfile.SetValue("MemberType", data.MemberType);
						sfProfile.SetValue("Status", data.Status);
					}
					uMan.SaveChanges();
					upMan.RecompileItemUrls(sfProfile);
					upMan.SaveChanges();

					var roleManager = RoleManager.GetManager();
					roleManager.Provider.SuppressSecurityChecks = true;
					var memberRole = roleManager.GetRole("Member");
					roleManager.AddUserToRole(sfUser, memberRole);

					if (data.MemberType.ToLower() == "m" || data.MemberType.ToLower() == "staff")
					{
						var activeMemberRole = roleManager.GetRole("SSO Member");
						roleManager.AddUserToRole(sfUser, activeMemberRole);
					}

					roleManager.SaveChanges();
				}
				// Log the status if the create failed

			}
			else
			{

				var sfProfile = upMan.GetUserProfile<SitefinityProfile>(sfUser);
				var saveChanges = false;
				if (sfProfile.FirstName != data.FirstName)
				{
					sfProfile.FirstName = data.FirstName;
					saveChanges = true;
				}
				if (sfProfile.LastName != data.LastName)
				{
					sfProfile.LastName = data.LastName;
					saveChanges = true;
				}
				if (sfProfile.GetValue<String>("MiddleName") != data.MiddleName)
				{
					sfProfile.SetValue("MiddleName", data.MiddleName);
					saveChanges = true;
				}
				if (sfProfile.GetValue<String>("ImisId") != data.ImisId)
				{
					sfProfile.SetValue("ImisId", data.ImisId);
					saveChanges = true;
				}
				if (sfProfile.GetValue<String>("MemberType") != data.MemberType)
				{
					sfProfile.SetValue("MemberType", data.MemberType);
					saveChanges = true;
				}
				if (sfProfile.GetValue<String>("Status") != data.Status)
				{
					sfProfile.SetValue("Status", data.Status);
					saveChanges = true;
				}

				var roleManager = RoleManager.GetManager();
				roleManager.Provider.SuppressSecurityChecks = true;
				var activeMemberRole = roleManager.GetRole("SSO Member");
				if (data.MemberType.ToLower() == "m" || data.MemberType.ToLower() == "staff")
				{
					roleManager.AddUserToRole(sfUser, activeMemberRole);
				}
				else
				{
					roleManager.RemoveUserFromRole(sfUser, activeMemberRole);
				}
				roleManager.SaveChanges();

				if (saveChanges)
				{
					upMan.RecompileItemUrls(sfProfile);
					upMan.SaveChanges();
				}
			}

			return sfUser;
		}

		protected void PrepareWorkflowPanels(bool isAdmin, UserLoggingReason reason)
		{
			Type SecurityManagerClass = Type.GetType("Telerik.Sitefinity.Security.SecurityManager, Telerik.Sitefinity");
			var exposedSecurityManagerClass = ExposedObject.Exposed.From(SecurityManagerClass);

			Type LoginFormClass = Type.GetType("Telerik.Sitefinity.Security.Web.UI.LoginForm, Telerik.Sitefinity");

			LoginPanel.Visible = false;
			//var ticket = BuildAuthTicket(isAdmin);
			var ticket = LoginFormClass.GetMethod("BuildAuthTicket", (BindingFlags.NonPublic | BindingFlags.Instance)).Invoke(this, new object[] { isAdmin }) as string;
			this.AuthTicket = ticket;

			Type UserActivityManagerClass = Type.GetType("Telerik.Sitefinity.Security.UserActivityManager, Telerik.Sitefinity");
			dynamic uaManager = UserActivityManagerClass.GetMethod("GetManager", new Type[0]).Invoke(null, new object[0]);
			var userActivity = uaManager.Provider.GetUserActivity(this.currentUser.Id, this.currentUser.ProviderName);

			//The maximum allowed logged in users limit is reached.
			//Administrator should choose instead of who will log in. 
			//The selected user will be logged off.
			if (reason == UserLoggingReason.UserLimitReached)
			{
				if (isAdmin)
				{
					this.Mode = AdminLogsOutUser;
					BindLoggedInUsersList();
					UserListPanel.Visible = true;
					return;
				}

				DisplayDenyLogin();
			}
			else if (userActivity.LastActivityDate >= exposedSecurityManagerClass.ExpiredSessionsLastLoginDate)
			{
				//This case is where you want to logout yourself from different computer or browser
				//SetSelfLogoutMode(this.currentUser, UserAlreadyLoggedIn);
				LoginFormClass.GetMethod("SetSelfLogoutMode", (BindingFlags.NonPublic | BindingFlags.Instance)).Invoke(this, new object[] { this.currentUser, UserAlreadyLoggedIn });
			}
			else
			{
				//When the session is expired just force user to log in with message
				this.Mode = string.Empty;
				LoginPanel.Visible = true;
				SelfLogoffPanel.Visible = false;
			}
		}
	}
}
