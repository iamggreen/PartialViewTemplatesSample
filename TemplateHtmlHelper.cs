using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Collections.Concurrent;
using System.Text;

namespace PartialViewTemplatesSample
{
    public static class TemplateHtmlHelper
    {
        private static ConcurrentDictionary<string, IEnumerable<string>> _fileListCache = new ConcurrentDictionary<string, IEnumerable<string>>();

        /// <summary>
        /// Returns each file named Template.* in the given directory wrapped in a templated script tag.
        /// This will cache the list of files in the directory so we do not have to read it on every call
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static MvcHtmlString RenderAllTemplates(this HtmlHelper helper, string directory)
        {

            IEnumerable<string> fileList;

            if (!_fileListCache.TryGetValue(directory, out fileList))
            {
                var physicalPath = helper.ViewContext.RequestContext.HttpContext.Server.MapPath(directory);
                fileList = Directory.GetFiles(physicalPath, "*Template.*")
                                    .Select(x => Path.GetFileNameWithoutExtension(x));
                _fileListCache.GetOrAdd(directory, fileList);
            }

            var idPrefix = helper.ViewContext.RouteData.Values["controller"] + "-";
            idPrefix = idPrefix.ToLower();

            var result =  string.Join("\n", fileList.Select(x => RenderTemplateToString(helper, idPrefix + GetIdFromPartialViewName(x), x)).ToArray());

            return new MvcHtmlString(result);
        }

        /// <summary>
        /// Returns the given partial view, wrapped in a templated script tag
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="templateId"></param>
        /// <param name="partialViewName"></param>
        /// <returns></returns>
        public static MvcHtmlString RenderTemplate(this HtmlHelper helper, string templateId, string partialViewName)
        {
            var template = RenderTemplateToString(helper, templateId, partialViewName);
           return new MvcHtmlString(template);
        }

        /// <summary>
        /// Returns the partial view to a string and wraps it inside of a templated script tag
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="templateId"></param>
        /// <param name="partialViewName"></param>
        /// <returns></returns>
        private static string RenderTemplateToString(HtmlHelper helper, string templateId, string partialViewName)
        {
            var template = string.Format("<script type=\"text/template\" id=\"{0}\">{1}</script>",
                                         templateId,
                                         RenderPartialToString(helper, partialViewName));

            return template;
        }


        /// <summary>
        /// Returns the partial view and returns the resulting string
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="partialViewName"></param>
        /// <returns></returns>
        private static string RenderPartialToString(HtmlHelper helper, string partialViewName)
        {
            var controllerContext = helper.ViewContext.Controller.ControllerContext;
            var viewData = helper.ViewData;
            var tempData = helper.ViewContext.TempData;

            using (var stringWriter = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(controllerContext, partialViewName);

                var viewContext = new ViewContext(controllerContext, viewResult.View, viewData, tempData, stringWriter);

                viewResult.View.Render(viewContext, stringWriter);

                return stringWriter.GetStringBuilder().ToString();
            }

            
        }

        /// <summary>
        /// Creates an id for the script tag element from the partial view name.
        /// This method assumes the partial view name is camel case and the 
        /// resulting id will be all lower case with the words separated by a hyphen
        /// i.e. in: MyFirstTemplate out: my-first-template
        /// </summary>
        /// <param name="partialViewName"></param>
        /// <returns></returns>
        private static string GetIdFromPartialViewName(string partialViewName)
        {
            var result = new StringBuilder();

            for (int i = 0; i < partialViewName.Length; i++)
            {
                if (i > 0 && char.IsUpper(partialViewName[i]))
                    result.Append("-").Append(char.ToLower(partialViewName[i]));
                else
                    result.Append(char.ToLower(partialViewName[i]));
            }

            return result.ToString();
        }
    }
}