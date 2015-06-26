<%@ Assembly Name="$SharePoint.Project.AssemblyFullName$" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="asp" Namespace="System.Web.UI" Assembly="System.Web.Extensions, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" %>
<%@ Import Namespace="Microsoft.SharePoint" %> 
<%@ Register Tagprefix="WebPartPages" Namespace="Microsoft.SharePoint.WebPartPages" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Scripts.ascx.cs" Inherits="iVision.Branding.ControlTemplates.iVision.Branding.Scripts" %>

<SharePoint:CssRegistration ID="BootstrapCSS" Name="<% $SPUrl:~Site/_layouts/15/iVision.Branding/css/bootstrap.min.css %>" after="Themable/corev15.css" runat="server"></SharePoint:CssRegistration>
<SharePoint:CssRegistration ID="BootstrapTheme" Name="<% $SPUrl:~Site/_layouts/15/iVision.Branding/css/bootstrap-theme.css %>" after="Themable/corev15.css" runat="server"></SharePoint:CssRegistration>
<SharePoint:CssRegistration ID="FontAwesome" Name="<% $SPUrl:~Site/_layouts/15/iVision.Branding/css/font-awesome.min.css %>" after="Themable/corev15.css" runat="server"></SharePoint:CssRegistration>

<script type="text/javascript">
    // Bootstrap depends on jQuery
    // @todo Type.RegisterNamespace using ExecuteFunc()
    RegisterSodDep('jQuery', 'init.js');
    RegisterSodDep('Bootstrap', 'jQuery');
</script>
<SharePoint:ScriptLink ID="jQuery" OnDemand="false" Name="/_layouts/15/iVision.Branding/js/jquery.min.js" runat="server" />
<SharePoint:ScriptLink ID="Bootstrap" OnDemand="false" Name="/_layouts/15/iVision.Branding/js/bootstrap.min.js" runat="server" />