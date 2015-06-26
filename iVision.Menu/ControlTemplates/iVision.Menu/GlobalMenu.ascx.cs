using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Publishing.Navigation;
using Microsoft.SharePoint.Taxonomy;
using Microsoft.SharePoint.Taxonomy.OM.CodeBehind;
using Microsoft.SharePoint.Utilities;

namespace iVision.Menu.ControlTemplates.iVision.Menu
{
    [
        ControlValueProperty("SelectedValue"),
        DefaultEvent("MenuItemClick"),
        SupportsEventValidation,
        ParseChildren(true, "TermSetID"),
        ToolboxData("<{0}:GlobalMenu runat=\"server\"></{0}:GlobalMenu>")
    ]
    public partial class GlobalMenu : UserControl
    {
        // PARAMETERS
        public static string Html { get; set; }
        public static HtmlTextWriter HtmlWriter { get; set; }
        // Tracks level of the menu (level 1 = Top Level)
        public static int Level { get; set; }
        // Tracks whether to show child term items
        public static bool ShowChildren { get; set; }
        public string SiteUrl { get; set; }
        public string TermSetId { get; set; }
        //tsid=########-####-####-####-############
        // Our expected Term Store ID is: 9418c8f1-96ec-4a7b-8499-f4d5df4fd84f
        public string TermStoreId { get; set; }
        // Private Properties
        private string CssClass { get; set; }
        private MenuItem CurrentItem { get; set; }
        private CurrentTermSet CTermSet { get; set; }
        private SPSite Site { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Create a monitored scope for the developer dashboard, etc.
            using (new SPMonitoredScope("iVision.Menu.ControlTemplates.iVision.Menu.GlobalMenu::Page_Load"))
            {
                if (!Page.IsPostBack)
                {
                    Level = 0;
                    SPSecurity.RunWithElevatedPrivileges(delegate
                    {
                        using (
                            var thisSite =
                                new SPSite(SPContext.Current.Site.WebApplication.AlternateUrls[0].Uri.AbsoluteUri))
                        {
                            Site = thisSite;
                            // Get Taxonomy Session, Term Store
                            var session = new TaxonomySession(thisSite);

                            // Use the first TermStore in the list
                            if (session.TermStores.Count == 0)
                            {
                                throw new InvalidOperationException("The Taxonomy Service is offline or missing");
                            }

                            // Initialize StringWriter instance.
                            var stringWriter = new StringWriter();

                            // Put HtmlTextWriter in using block because it needs to call Dispose.
                            using (var writer = new HtmlTextWriter(stringWriter))
                            {
                                TermSet termSetForNav;
                                var stores = session.TermStores;
                                var termStoreGuid = Guid.Empty;
                                if (Guid.TryParse(TermStoreId, out termStoreGuid))
                                {
                                    var termStore = TermStoreManager.GetTermStore(session, termStoreGuid);
                                    var termSetGuid = Guid.Empty;
                                    if (Guid.TryParse(TermSetId, out termSetGuid))
                                    {
                                        termSetForNav = termStore.GetTermSet(termSetGuid);
                                        if (null == termSetForNav)
                                        {
                                            //termSetForNav = termStore.Groups["Navigation"].TermSets["Global"];
                                        }
                                    }
                                    else
                                    {
                                        //nTermSet = termStore.Groups["Navigation"].TermSets.GetByName("Global");
                                        termSetForNav = termStore.Groups["Navigation"].TermSets["Global"];
                                    }
                                }
                                else
                                {
                                    termSetForNav = stores[0].Groups["Navigation"].TermSets["Global"];
                                }

                                if (null == termSetForNav)
                                {
                                    //return;
                                }
                                var navigationTermSet = NavigationTermSet.GetAsResolvedByWeb(termSetForNav,
                                    thisSite.OpenWeb(), StandardNavigationProviderNames.GlobalNavigationTaxonomyProvider);

                                this.CTermSet = new CurrentTermSet
                                {
                                    TermSet = termSetForNav,
                                    NavigationTermSet = navigationTermSet
                                };

                                // Begin nav output
                                CssClass = "nav navbar-nav";
                                //writer.AddAttribute("role", "navigation");
                                writer.AddAttribute(HtmlTextWriterAttribute.Class, CssClass);
                                writer.RenderBeginTag(HtmlTextWriterTag.Ul);

                                // check nTermSet somehow
                                //buildItems(writer, navTermSet.Terms);
                                //this.buildItems(writer, cTermSet.getTerms());
                                this.BuildItems(writer, navigationTermSet);

                                writer.RenderEndTag();
                            }

                            // Return the result.
                            GlobalMenuContainer.Text = "";
                            GlobalMenuContainer.Text = stringWriter.ToString();
                        }
                    });
                }
            }
        }

        // previously returned string
        //public HtmlTextWriter buildItems(HtmlTextWriter writer, TermCollection terms)
        public HtmlTextWriter BuildItems(HtmlTextWriter writer, NavigationTermSet navTerms)
        {
            //if (terms.Count > 0)

            // Don't need this and maybe deprecate
            //var termSet = navTerms.GetTaxonomyTermSet();

            if (navTerms.IsNavigationTermSet)
            {
                Level++;

                //foreach (Term term in terms)
                //foreach (NavigationTerm navTerm in navTerms)
                for (var i = 0; i <= navTerms.Terms.Count; i++)
                {
                    try
                    {
                        var navTerm = navTerms.Terms[i];
                        if (navTerm.ExcludeFromGlobalNavigation)
                        {
                            continue;
                        }
                        //buildItem(writer, term);
                        buildItem(writer, navTerm);
                    }
                    catch (Exception)
                    {
                        // Add Exception stuff here...
                    }
                }
                Level--;

                //html += "</ul>\n";
            }
            return writer;
        }

        public HtmlTextWriter buildItems(HtmlTextWriter writer, TermSet termSet)
        {
            var navTermSet = NavigationTermSet.GetAsResolvedByWeb(termSet, Site.OpenWeb(),
                StandardNavigationProviderNames.GlobalNavigationTaxonomyProvider);
            return BuildItems(writer, navTermSet);
        }

        public HtmlTextWriter buildItems(HtmlTextWriter writer, TermCollection terms)
        {
            //TermSet termSet = navTerms.GetTaxonomyTermSet();

            //if (navTerms.IsNavigationTermSet)
            if (terms.Count > 0)
            {
                Level++;

                foreach (var term in terms)
                {
                    try
                    {
                        var navTerm = NavigationTerm.GetAsResolvedByWeb(term, Site.OpenWeb(),
                            StandardNavigationProviderNames.GlobalNavigationTaxonomyProvider);
                        if (navTerm.ExcludeFromGlobalNavigation)
                        {
                            continue;
                        }

                        buildItem(writer, navTerm);
                    }
                    catch (Exception)
                    {
                    }
                }
                Level--;
            }
            return writer;
        }

        // Primary BuildItem
        // Outputs a single menu item or menu item + sub-tree
        //public void buildItem(HtmlTextWriter writer, Term term)
        public void buildItem(HtmlTextWriter writer, NavigationTerm navTerm)
        {
            var temp = "";
            var term = navTerm.GetTaxonomyTerm();

            // Check permissions??
            //if (!term.DoesUserHavePermissions())
            //{
            //return;
            //}

            // Instantiate current item
            CurrentItem = new MenuItem();
            CurrentItem.Term = new CurrentTerm(term, navTerm);
            CurrentItem.TermSet = CTermSet;

            //temp = htmlBeforeLi
            if (term.CustomProperties.TryGetValue("htmlBeforeLi", out temp))
            {
                writer.Write(temp);
            }

            // MegaMenu classes: dropdown-wide, dropdown-short, dropdown-full, dropdown-onhover, dropdown-menu, dropdown-carousel, dropdwn-grid
            if (term.CustomProperties.TryGetValue("itemClasses", out temp))
            {
                CurrentItem.AddClass(temp);
            }

            if (term.CustomProperties.TryGetValue("columns", out temp))
            {
                CurrentItem.AddClass("columns");
                CurrentItem.AddClass("columns-" + temp);
            }

            if (term.CustomProperties.TryGetValue("iconOnly", out temp))
            {
                CurrentItem.AddClass(temp);
                CurrentItem.AddClass("icon-only");
                CurrentItem.AddClass("image-icon-only");
            }
            else if (term.CustomProperties.TryGetValue("imageOnly", out temp))
            {
                CurrentItem.AddClass(temp);
                CurrentItem.AddClass("image-only");
                CurrentItem.AddClass("image-icon-only");
            }

            // Add level class
            CurrentItem.Level = Level;

            if (HasChildren(term))
            {
                CurrentItem.AddClass("dropdown");

                if (term.CustomProperties.ContainsKey("megamenu"))
                {
                    CurrentItem.AddClass("imegamenu");
                    CurrentItem.AddClass("dropdown-short");
                }

                ShowChildren = false;
                if (!term.CustomProperties.ContainsKey("showChildren"))
                {
                    if (1 < Level)
                    {
                        CurrentItem.AddClass("dropdown-right-onhover");
                        CurrentItem.AddClass("no-fix");
                    }
                    else
                    {
                        CurrentItem.AddClass("dropdown-onhover");
                    }
                }
                else
                {
                    ShowChildren = true;
                    CurrentItem.AddClass("dropdown-onhover");
                    CurrentItem.AddClass("dropdown-show");
                }


                // If dropdown, add attributes
                //writer.AddAttribute("data-toggle", "dropdown");


                // @todo Add style attribute
                if (term.CustomProperties.TryGetValue("style", out temp))
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Style, temp);
                }

                // @todo Add data tags, entered name: data-name value: *
                // Can I get all properties that begin with a string??
                //if (term.CustomProperties.TryGetValue("data", out temp)) {
                //writer.AddAttribute("", currentItem.GetStyles());
                //}
            }

            writer.AddAttribute(HtmlTextWriterAttribute.Class, CurrentItem.GetClasses());
            writer.RenderBeginTag(HtmlTextWriterTag.Li);

            if (term.CustomProperties.TryGetValue("htmlBefore", out temp))
            {
                writer.Write(temp);
            }

            //if (this.hasChildren(term) && term.CustomProperties.ContainsKey("showChildren"))
            if (HasChildren(term))
            {
                RenderDropDown(writer, term, navTerm);
            }
            else
            {
                RenderLink(writer, term, navTerm);
            }

            //writer.Write(this.getValue(term));
            //writer.Write(this.getDescription(term));


            if (term.CustomProperties.TryGetValue("htmlAfter", out temp))
            {
                writer.Write(temp);
            }

            // Now 
            writer.RenderEndTag(); // </li>

            if (term.CustomProperties.TryGetValue("htmlAfterLi", out temp))
            {
                writer.Write(temp);
            }
        }

        public void buildItem(HtmlTextWriter writer, Term term)
        {
            var navTerm = NavigationTerm.GetAsResolvedByWeb(term, Site.OpenWeb(),
                StandardNavigationProviderNames.GlobalNavigationTaxonomyProvider);
            buildItem(writer, navTerm);
        }

        private bool HasChildren(Term term)
        {
            _checkParam(term, "term");

            return (term.TermsCount > 0);
        }

        // Gets Link Value: Text only, Icon only, Text + Icon, Text + Icon + Description
        private string GetValue(Term term)
        {
            var value = term.Name;
            var icon = "";
            var image = "";
            var position = "";

            // Check for icon
            if (term.CustomProperties.TryGetValue("icon", out icon))
            {
                if (term.CustomProperties.ContainsKey("iconOnly"))
                {
                    value = GetIconHtml(icon);
                }
                else
                {
                    // Get position: Before/After (defaults to before)
                    value = GetIconHtml(image) + "&nbsp;" + term.Name;
                    if (term.CustomProperties.TryGetValue("iconPosition", out position))
                    {
                        if ("after" == position)
                        {
                            value = term.Name + "&nbsp;" + GetImgHtml(image, term);
                        }
                    }
                }
            }

            // Check for image
            if (term.CustomProperties.TryGetValue("image", out image))
            {
                if (term.CustomProperties.ContainsKey("imageOnly"))
                {
                    value = GetImgHtml(image, term);
                }
                else
                {
                    // Get position: Before/After (defaults to before)
                    value = GetImgHtml(image, term) + "&nbsp;" + term.Name;
                    if (term.CustomProperties.TryGetValue("imagePosition", out position))
                    {
                        if ("after" == position)
                        {
                            value = term.Name + "&nbsp;" + GetImgHtml(image, term);
                        }
                    }
                }
            }

            if (term.CustomProperties.ContainsKey("showDescription"))
            {
                value += GetDescription(term);
            }

            return value;
        }

        private string GetImgHtml(string imageSrc, Term term)
        {
            // Initialize StringWriter instance.
            var stringWriter = new StringWriter();

            // Put HtmlTextWriter in using block because it needs to call Dispose.
            using (var writer = new HtmlTextWriter(stringWriter))
            {
                var classes = "";
                var imageAlt = "";
                term.CustomProperties.TryGetValue("imageClass", out classes);
                classes = string.Concat(classes, " img-responsive img-center");
                term.CustomProperties.TryGetValue("imageTitle", out imageAlt);

                writer.AddAttribute(HtmlTextWriterAttribute.Alt, imageAlt, true); // Add required Alt
                writer.AddAttribute(HtmlTextWriterAttribute.Class, classes); // Add class
                writer.AddAttribute(HtmlTextWriterAttribute.Src, imageSrc);
                writer.RenderBeginTag(HtmlTextWriterTag.Img); // Begin <i>
                writer.RenderEndTag(); // End </i>
            }

            // Return the result.
            return stringWriter.ToString();
        }

        private string GetDescription(Term term)
        {
            var value = "";

            if (term.CustomProperties.ContainsKey("showDescription") && !term.GetDescription().Equals(string.Empty))
            {
                // @todo Fix with writer and tagWriter method; allow HTML or sanitize?
                value += "<span class=\"description desc\">" + term.GetDescription() + "</span>";
            }

            return value;
        }

        private string GetIconHtml(string icon, string addClasses = "")
        {
            // Initialize StringWriter instance.
            var stringWriter = new StringWriter();

            // Put HtmlTextWriter in using block because it needs to call Dispose.
            using (var writer = new HtmlTextWriter(stringWriter))
            {
                var classes = "fa " + icon;
                if (!string.IsNullOrEmpty(addClasses))
                {
                    classes += " " + addClasses;
                }
                writer.AddAttribute(HtmlTextWriterAttribute.Class, classes); // Add class
                writer.RenderBeginTag(HtmlTextWriterTag.I); // Begin <i>
                writer.RenderEndTag(); // End </i>
            }

            // Return the result.
            return stringWriter.ToString();
        }

        private void RenderLink(HtmlTextWriter writer, Term term, NavigationTerm navTerm)
        {
            _checkParam(writer, "writer");
            _checkParam(term, "term");

            /*
              try
              {
                  html += "<li><a href=\"" + term.LocalCustomProperties["_Sys_Nav_SimpleLinkUrl"] + "\">" + term.Name + "</a>";
                  writeTermsHTML(term.Terms);
                  html += "</li>\n";
              }
              catch (Exception)
              {
                  html += "<li><a href=\"#\">" + term.Name + "</a>";
                  writeTermsHTML(term.Terms);
                  html += "</li>\n";
              }
              */

            writer.AddAttribute(HtmlTextWriterAttribute.Href, GetLinkUrl(navTerm));

            var temp = "";
            if (term.CustomProperties.TryGetValue("header", out temp))
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Li);
                writer.Write(temp);
                writer.RenderEndTag();
                return;
            }

            // Add ToolTip
            // @todo Make toolTip a parameter
            //string toolTip = !string.IsNullOrEmpty(item.ToolTip) ? item.ToolTip : item.Text;
            var toolTip = !string.IsNullOrEmpty(navTerm.HoverText) ? navTerm.HoverText : term.Name;
            term.CustomProperties.TryGetValue("toolTip", out toolTip);
            writer.AddAttribute(HtmlTextWriterAttribute.Title, toolTip);

            if (!term.CustomProperties.ContainsKey("iconOnly") || !term.CustomProperties.ContainsKey("imageOnly"))
            {
                writer.RenderBeginTag(HtmlTextWriterTag.A);
            }

            writer.Write(GetValue(term));

            if (!term.CustomProperties.ContainsKey("iconOnly") || !term.CustomProperties.ContainsKey("imageOnly"))
            {
                writer.RenderEndTag(); // </a>
            }
        }

        private string GetLinkUrl(NavigationTerm navTerm)
        {
            var href = "#";
            var linkType = navTerm.LinkType;
            if (NavigationLinkType.FriendlyUrl == linkType)
            {
                //href = navTerm.GetResolvedDisplayUrl();
                href = navTerm.FriendlyUrlSegment.ToString();
            }
            else
            {
                href = navTerm.SimpleLinkUrl;
            }
            return href;
        }

        private void RenderDropDown(HtmlTextWriter writer, Term term, NavigationTerm navTerm)
        {
            _checkParam(writer, "writer");

            writer.AddAttribute(HtmlTextWriterAttribute.Href, GetLinkUrl(navTerm));
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "dropdown-toggle");
            writer.AddAttribute("data-toggle", "dropdown");
            writer.AddAttribute("data-dropdown", "hover");

            if (!term.CustomProperties.ContainsKey("iconOnly") || !term.CustomProperties.ContainsKey("imageOnly"))
            {
                writer.RenderBeginTag(HtmlTextWriterTag.A); //<a>
            }
            //writer.RenderBeginTag(HtmlTextWriterTag.A); // <a>

            var anchorValue = GetValue(term);
            if (1 == Level)
            {
                anchorValue += "&nbsp;";
            }
            writer.Write(anchorValue);

            if (1 == Level)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "caret");
                writer.RenderBeginTag(HtmlTextWriterTag.B); // <b>
                writer.RenderEndTag(); // </b>
            }

            if (!term.CustomProperties.ContainsKey("iconOnly") || !term.CustomProperties.ContainsKey("imageOnly"))
            {
                writer.RenderEndTag(); // </a>
            }
            //writer.RenderEndTag(); // </a>

            writer.AddAttribute(HtmlTextWriterAttribute.Class, "dropdown-menu pull-center");
            writer.RenderBeginTag(HtmlTextWriterTag.Ul); // <ul>

            // Maybe add Custom Properties
            if (ShowChildren)
            {
                var counter = 0;
                /*
                foreach (Term t in term.Terms)
                {
                    counter++;
                    if (!t.CustomProperties.ContainsKey("itemClasses"))
                    {
                        term.Terms[counter - 1].SetCustomProperty("itemClasses", "imegamenu-content");
                    }
                    else
                    {
                        var temp = "";
                        t.CustomProperties.TryGetValue("itemClasses", out temp);
                        term.Terms[counter - 1].SetCustomProperty("itemClasses", temp);
                    }
                    if (1 == counter && !t.CustomProperties.ContainsKey("htmlBeforeLi"))
                    {
                        //term.Terms[counter - 1].SetCustomProperty("htmlBeforeLi", "<li><div class=\"imegamenu-content\"><div class=\"row\">");
                    }
                    else if (term.Terms.Count == counter && !t.CustomProperties.ContainsKey("htmlAfterLi"))
                    {
                        //term.Terms[counter - 1].SetCustomProperty("htmlAfterLi", "</div></div></li>");
                    }
                }
                 * */
            }

            //this.buildItems(writer, term.Terms);
            //this.buildItems(writer, navTerm);
            //this.buildItems(writer, navTerm.b);
            buildItems(writer, term.Terms);

            //TermSet childTermSet = (TermSet) cTermSet.termSet.GetTerms();

            //this.buildItems(writer,);
            writer.RenderEndTag(); // </ul>
        }

        private void _checkParam(dynamic param, string exception = "")
        {
            return;
            if (param == null)
            {
                throw new ArgumentNullException(exception);
            }
        }
    }


    public class CurrentTermSet
    {
        public TermSet TermSet { get; set; }
        public NavigationTermSet NavigationTermSet { get; set; }
        public NavigationTerm RootNavigationTerm { get; set; }

        public TermCollection GetTerms()
        {
            return TermSet.Terms;
        }
    }

    public class CurrentTerm
    {
        public CurrentTerm(Term term, NavigationTerm navigationTerm)
        {
        }

        public Term Term { get; set; }
        public NavigationTerm NavigationTerm { get; set; }
    }

    public class MenuItem
    {
        // Classes
        private readonly List<string> _classes = new List<string>();
        // Styles
        //private string Styles { get; set; }
        private readonly StyleCollection _styles = new StyleCollection();
        // Data
        private DataCollection _data = new DataCollection();
        private int _level;
        private Term _term;

        public MenuItem()
        {
            Offset = 0;
            Columns = 0;
        }

        public CurrentTerm Term { get; set; }
        public CurrentTermSet TermSet { get; set; }
        // Level
        public int Level
        {
            get { return _level; }

            set
            {
                if (0 != value)
                {
                    AddClass("level-" + value);
                    RemoveClass("level-" + _level);
                }
                _level = value;
            }
        }

        // Columns
        public int Columns { get; set; }
        // Offset
        public int Offset { get; set; }
        /** Columns Methods **/
        // @todo Fix this to allow for different columns based on viewports (e.g.,col-xs-*, col-sm-*, col-md-*, col-lg-*)
        public void AddColumnClasses()
        {
            // 1, 2, 3, 4, 
            if (0 == Columns || 5 == Columns || 6 < Columns)
            {
                return;
            }
            var c = 12/Columns;
            switch (Columns)
            {
                case 1:
                    AddClass("col-xs-12");
                    break;
                case 2:
                    AddClass("col-xs-6");
                    break;
                case 3:
                    AddClass("col-xs-4");
                    break;
                case 4:
                    AddClass("col-xs-3");
                    break;
                case 6:
                    AddClass("col-xs-2");
                    break;
                default:
                    // do nothing
                    break;
            }
        }

        // @todo Fix this to allow for different offsets based on viewports (e.g.,col-xs-*, col-sm-*, col-md-*, col-lg-*)
        public string AddOffsetClasses()
        {
            // 1, 2, 3, 4, 
            if (0 == Offset)
            {
                return "";
            }

            return "col-sm-offset-" + Offset;
        }

        /** Classes Methods **/
        // Adds Class
        public void AddClass(string className)
        {
            if (className.Contains(" "))
            {
                _classes.AddRange(className.Split(' '));
            }
            else
            {
                _classes.Add(className);
            }
        }

        // Removes Class
        public void RemoveClass(string className)
        {
            if (_classes.Contains(className))
            {
                _classes.Remove(className);
            }
        }

        // Gets Classes as string
        public string GetClasses()
        {
            return StripMultipleSpaces(string.Join(" ", _classes.Distinct().ToList().ToArray()));
        }

        // Internal Utilities
        private string StripMultipleSpaces(string str)
        {
            return Regex.Replace(str, @"\s+", " ");
        }

        /** Styles Methods **/

        public void AddStyle(Style style)
        {
            _styles.Add(style);
        }

        public void AddStyle(string property, string value)
        {
            _styles.Add(new Style(property, value));
        }

        public void RemoveStyle(string property)
        {
            if (_styles.Contains(property))
            {
                _styles.Remove(property);
            }
        }

        public string GetStyles()
        {
            var output = "";
            foreach (var item in _styles)
            {
                output = string.Concat(output, item.ToString());
            }
            return output;
        }
    }

    public class Style : Attribute
    {
        public Style(string property, string value) : base(property, value)
        {
        }

        public override string ToString()
        {
            return string.Concat(new[] {GetProperty(), ":", GetValue(), ";"});
        }
    }

    public class StyleCollection : KeyedCollection<string, Style>
    {
        // Necessary override method 
        protected override string GetKeyForItem(Style item)
        {
            return item.GetProperty();
        }
    }

    public class Data : Attribute
    {
        public Data(string property, string value) : base(property, value)
        {
        }

        public override string ToString()
        {
            return string.Concat("data-", GetProperty(), "=\"", GetValue(), "\"");
        }
    }

    public class DataCollection : KeyedCollection<string, Style>
    {
        // Necessary override method 
        protected override string GetKeyForItem(Style item)
        {
            return item.GetProperty();
        }
    }

    public abstract class Attribute
    {
        public Attribute(string property, string value)
        {
            Property = property;
            Value = value;
        }

        private string Property { get; set; }
        private string Value { get; set; }

        public void Set(string val)
        {
            Value = val;
        }

        public string GetProperty()
        {
            return Property;
        }

        public string GetValue()
        {
            return Value;
        }

        public new abstract string ToString();
    }
}