// MsdnDocumenter.cs - a MSDN-like documenter
// Copyright (C) 2001  Kral Ferch, Jason Diamond
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Globalization;

using NDoc3.Core;
using NDoc3.Core.Reflection;
using NDoc3.Documenter.Msdn.onlinefiles;
using NDoc3.Documenter.Msdn.onlinetemplates;
using NDoc3.Xml;

namespace NDoc3.Documenter.Msdn
{
	/// <summary>The MsdnDocumenter class.</summary>
	public class MsdnDocumenter : BaseReflectionDocumenter
	{
		private enum WhichType
		{
			Class,
			Interface,
			Structure,
			Enumeration,
			Delegate,
			Unknown
		};

		private readonly Dictionary<WhichType, string> lowerCaseTypeNames;
		private readonly Dictionary<WhichType, string> mixedCaseTypeNames;
		private List<string> filesToInclude = new List<string>();

		/// <summary>
		/// Initializes a new instance of the <see cref="MsdnDocumenter" />
		/// class.
		/// </summary>
		public MsdnDocumenter(MsdnDocumenterConfig config)
			: base(config)
		{
			lowerCaseTypeNames = new Dictionary<WhichType, string>();
			lowerCaseTypeNames.Add(WhichType.Class, "class");
			lowerCaseTypeNames.Add(WhichType.Interface, "interface");
			lowerCaseTypeNames.Add(WhichType.Structure, "structure");
			lowerCaseTypeNames.Add(WhichType.Enumeration, "enumeration");
			lowerCaseTypeNames.Add(WhichType.Delegate, "delegate");

			mixedCaseTypeNames = new Dictionary<WhichType, string>();
			mixedCaseTypeNames.Add(WhichType.Class, "Class");
			mixedCaseTypeNames.Add(WhichType.Interface, "Interface");
			mixedCaseTypeNames.Add(WhichType.Structure, "Structure");
			mixedCaseTypeNames.Add(WhichType.Enumeration, "Enumeration");
			mixedCaseTypeNames.Add(WhichType.Delegate, "Delegate");
		}

		/// <summary>See <see cref="IDocumenter"/>.</summary>
		public override string MainOutputFile
		{
			get
			{
				if ((MyConfig.OutputTarget & OutputType.HtmlHelp) > 0) {
					return Path.Combine(MyConfig.OutputDirectory,
						MyConfig.HtmlHelpName + ".chm");
				}
				return Path.Combine(MyConfig.OutputDirectory, "index.html");
			}
		}

		/// <summary>See <see cref="IDocumenter"/>.</summary>
		public override string CanBuild(Project project, bool checkInputOnly)
		{
			string result = base.CanBuild(project, checkInputOnly);
			if (result != null) {
				return result;
			}

			string AdditionalContentResourceDirectory = MyConfig.AdditionalContentResourceDirectory;
			if (AdditionalContentResourceDirectory.Length != 0 && !Directory.Exists(AdditionalContentResourceDirectory))
				return string.Format("The Additional Content Resource Directory {0} could not be found", AdditionalContentResourceDirectory);

			string ExtensibilityStylesheet = MyConfig.ExtensibilityStylesheet;
			if (ExtensibilityStylesheet.Length != 0 && !File.Exists(ExtensibilityStylesheet))
				return string.Format("The Extensibility Stylesheet file {0} could not be found", ExtensibilityStylesheet);

			if (checkInputOnly) {
				return null;
			}

			string path = Path.Combine(MyConfig.OutputDirectory,
				MyConfig.HtmlHelpName + ".chm");

			string temp = Path.Combine(MyConfig.OutputDirectory, "~chm.tmp");

			try {

				if (File.Exists(path)) {
					//if we can move the file, then it is not open...
					File.Move(path, temp);
					File.Move(temp, path);
				}
			} catch (Exception) {
				result = "The compiled HTML Help file is probably open.\nPlease close it and try again.";
			}

			return result;
		}

		/// <summary>See <see cref="IDocumenter"/>.</summary>
		public override void Build(Project project)
		{
			BuildProjectContext buildContext = new BuildProjectContext(new CultureInfo(MyConfig.LangID),
				new DirectoryInfo(MyConfig.OutputDirectory), MyConfig.CleanIntermediates);

			try {
				OnDocBuildingStep(0, "Initializing...");

				buildContext.Initialize();

				OnDocBuildingStep(10, "Merging XML documentation...");

				// Will hold the name of the file name containing the XML doc
				XmlDocument projectXml = CreateNDocXml(project);
				buildContext.SetProjectXml(projectXml, MyConfig.MergeAssemblies);

				OnDocBuildingStep(30, "Loading XSLT files...");

				buildContext.stylesheets = StyleSheetCollection.LoadStyleSheets(MyConfig.ExtensibilityStylesheet);

				OnDocBuildingStep(40, "Generating HTML pages...");

				// setup for root page
				string defaultTopic;
				string rootPageFileName = null;
				string rootPageTOCName = null;

				if (!String.IsNullOrEmpty(MyConfig.RootPageFileName)) {
					rootPageFileName = MyConfig.RootPageFileName;
					defaultTopic = "default.html";

					rootPageTOCName = "Overview";
					// what to call the top page in the table of contents?
					if (!String.IsNullOrEmpty(MyConfig.RootPageTOCName)) {
						rootPageTOCName = MyConfig.RootPageTOCName;
					}
				} else {
					// TODO (EE): check MergeAssemblies and adjust defaultTopic accordingly
					XmlNode defaultNamespace;
					if (MyConfig.MergeAssemblies)
					{
						XmlNodeList namespaceNodes = buildContext.SelectNodes("/ndoc:ndoc/ndoc:assembly/ndoc:module/ndoc:namespace");
						int[] indexes = SortNodesByAttribute(namespaceNodes, "name");

						defaultNamespace = namespaceNodes[indexes[0]];
					}
					else
					{
						XmlNodeList assemblyNodes = buildContext.SelectNodes("/ndoc:ndoc/ndoc:assembly");
						int[] assemblyIndexes = SortNodesByAttribute(assemblyNodes, "name");
						XmlNode defaultAssemblyNode = assemblyNodes[assemblyIndexes[0]];
						XmlNodeList namespaceNodes = buildContext.SelectNodes(defaultAssemblyNode, "ndoc:module/ndoc:namespace");
						int[] indexes = SortNodesByAttribute(namespaceNodes, "name");
						defaultNamespace = namespaceNodes[indexes[0]];
					}
					string defaultNamespaceName = GetNodeName(defaultNamespace);
					string assemblyName = GetNodeName(buildContext.SelectSingleNode(defaultNamespace, "ancestor::ndoc:assembly"));
					defaultTopic = buildContext._nameResolver.GetFilenameForNamespace(assemblyName, defaultNamespaceName);
				}
				buildContext.htmlHelp = SetupHtmlHelpBuilder(buildContext.WorkingDirectory, defaultTopic);

				using (buildContext.htmlHelp.OpenProjectFile())
				using (buildContext.htmlHelp.OpenContentsFile(string.Empty, true)) {
					// Write the embedded css files to the html output directory
					WriteHtmlContentResources(buildContext);

					GenerateHtmlContentFiles(buildContext, rootPageFileName, rootPageTOCName);
				}

				HtmlHelp htmlHelp = buildContext.htmlHelp;
				htmlHelp.WriteEmptyIndexFile();

				if ((MyConfig.OutputTarget & OutputType.Web) > 0) {
					OnDocBuildingStep(75, "Generating HTML index file...");

					// Write the embedded online templates to the html output directory
					GenerateHtmlIndexFile(buildContext, defaultTopic);
				}

				if ((MyConfig.OutputTarget & OutputType.HtmlHelp) > 0) {
					OnDocBuildingStep(85, "Compiling HTML Help file...");
					htmlHelp.CompileProject();
				}
#if !DEBUG
				else
				{
					//remove .hhc file
					File.Delete(htmlHelp.GetPathToContentsFile());
				}
#endif

				// if we're only building a CHM, copy that to the Outpur dir
				if ((MyConfig.OutputTarget & OutputType.HtmlHelp) > 0 && (MyConfig.OutputTarget & OutputType.Web) == 0) {
					buildContext.SaveOutputs("*.chm");
				} else {
					// otherwise copy everything to the output dir (cause the help file is all the html, not just one chm)
					buildContext.SaveOutputs("*.*");
				}

				OnDocBuildingStep(100, "Done.");
			} catch(DocumenterException) {
				throw;
			} catch (Exception ex) {
				throw new DocumenterException(ex.Message, ex);
			} finally {
				buildContext.Dispose();
			}
		}

		private void GenerateHtmlIndexFile(BuildProjectContext ctx, string defaultTopic)
		{
			EmbeddedResources.WriteEmbeddedResources(typeof(OnlineFilesLocationHint), ctx.WorkingDirectory);

			using (TemplateWriter indexWriter = new TemplateWriter(
				Path.Combine(ctx.WorkingDirectory.FullName, "index.html"),
				EmbeddedResources.GetEmbeddedResourceReader(typeof(OnlineTemplatesLocationHint), "index.html", null))) {
				indexWriter.CopyToLine("\t\t<title><%TITLE%></title>");
				indexWriter.WriteLine("\t\t<title>" + MyConfig.HtmlHelpName + "</title>");
				indexWriter.CopyToLine("\t\t<frame name=\"main\" src=\"<%HOME_PAGE%>\" frameborder=\"1\">");
				indexWriter.WriteLine("\t\t<frame name=\"main\" src=\"" + defaultTopic + "\" frameborder=\"1\">");
				indexWriter.CopyToEnd();
				indexWriter.Close();
			}

			Trace.WriteLine("transform the HHC contents file into html");
#if DEBUG
			int start = Environment.TickCount;
#endif
			//transform the HHC contents file into html
			using (StreamReader contentsFile = new StreamReader(ctx.HtmlHelpContentFilePath.FullName, ctx.CurrentFileEncoding)) {
				XPathDocument xpathDocument = new XPathDocument(contentsFile);
				string contentsFilename = Path.Combine(ctx.WorkingDirectory.FullName, "contents.html");
				using (StreamWriter streamWriter = new StreamWriter(
					File.Open(contentsFilename, FileMode.CreateNew, FileAccess.Write, FileShare.None), ctx.CurrentFileEncoding)) {
					XslTransform(ctx, "htmlcontents", xpathDocument, null, streamWriter, contentsFilename);
				}
			}
#if DEBUG
			Trace.WriteLine(string.Format("{0} msec.", (Environment.TickCount - start)));
#endif
		}

		private HtmlHelp SetupHtmlHelpBuilder(DirectoryInfo workingDirectory, string defaultTopic)
		{
			HtmlHelp htmlHelp = new HtmlHelp(
				workingDirectory,
				MyConfig.HtmlHelpName,
				defaultTopic,
				((MyConfig.OutputTarget & OutputType.HtmlHelp) == 0));
			htmlHelp.IncludeFavorites = MyConfig.IncludeFavorites;
			htmlHelp.BinaryTOC = MyConfig.BinaryTOC;
			htmlHelp.LangID = MyConfig.LangID;
			return htmlHelp;
		}

		private void GenerateHtmlContentFiles(BuildProjectContext buildContext, string rootPageFileName, string rootPageTOCName)
		{
			if (!String.IsNullOrEmpty(MyConfig.CopyrightHref)) {
				if (!MyConfig.CopyrightHref.StartsWith("http:")) {
					string copyrightFile = Path.Combine(buildContext.WorkingDirectory.FullName, Path.GetFileName(MyConfig.CopyrightHref));
					File.Copy(MyConfig.CopyrightHref, copyrightFile, true);
					File.SetAttributes(copyrightFile, FileAttributes.Archive);
					buildContext.htmlHelp.AddFileToProject(Path.GetFileName(MyConfig.CopyrightHref));
				}
			}

			// add root page if requested
			if (rootPageFileName != null) {
				if (!File.Exists(rootPageFileName)) {
					throw new DocumenterException("Cannot find the documentation's root page file:\n"
												  + rootPageFileName);
				}

				// add the file
				string rootPageOutputName = Path.Combine(buildContext.WorkingDirectory.FullName, "default.html");
				if (Path.GetFullPath(rootPageFileName) != Path.GetFullPath(rootPageOutputName)) {
					File.Copy(rootPageFileName, rootPageOutputName, true);
					File.SetAttributes(rootPageOutputName, FileAttributes.Archive);
				}
				buildContext.htmlHelp.AddFileToProject(Path.GetFileName(rootPageOutputName));
				buildContext.htmlHelp.AddFileToContents(rootPageTOCName,
										   Path.GetFileName(rootPageOutputName));

				// depending on peer setting, make root page the container
				if (MyConfig.RootPageContainsNamespaces)
					buildContext.htmlHelp.OpenBookInContents();
			}

			MakeHtmlForAssemblies(buildContext, MyConfig.MergeAssemblies);
			foreach (string filename in filesToInclude) {
				buildContext.htmlHelp.AddFileToProject(filename);
			}

			// close root book if applicable
			if (rootPageFileName != null) {
				if (MyConfig.RootPageContainsNamespaces)
					buildContext.htmlHelp.CloseBookInContents();
			}
		}

		private XmlDocument CreateNDocXml(Project project)
		{
			string tempFileName = null;
			try {
				// determine temp file name
				tempFileName = Path.GetTempFileName();
				// Let the Documenter base class do it's thing.
				MakeXmlFile(project, new FileInfo(tempFileName));

				// Load the XML documentation into DOM and XPATH doc.
				using (FileStream tempFile = File.Open(tempFileName, FileMode.Open, FileAccess.Read)) {

					XmlDocument xml = new XmlDocument();
					xml.Load(tempFile);
					return xml;
				}
			} finally {
				if (tempFileName != null && File.Exists(tempFileName)) {
#if DEBUG
					File.Copy(tempFileName, MyConfig.OutputDirectory.TrimEnd('\\', '/') + "\\ndoc.xml", true);
#endif
					File.Delete(tempFileName);
				}
			}
		}

		private void WriteHtmlContentResources(BuildProjectContext buildContext)
		{
			EmbeddedResources.WriteEmbeddedResources(
				GetType().Module.Assembly,
				GetType().Namespace + ".css",
				buildContext.WorkingDirectory);

			// Write the embedded icons to the html output directory
			EmbeddedResources.WriteEmbeddedResources(
				GetType().Module.Assembly,
				GetType().Namespace + ".images",
				buildContext.WorkingDirectory);

			// Write the embedded scripts to the html output directory
			EmbeddedResources.WriteEmbeddedResources(
				GetType().Module.Assembly,
				GetType().Namespace + ".scripts",
				buildContext.WorkingDirectory);

			if (((string)MyConfig.AdditionalContentResourceDirectory).Length > 0)
				buildContext.CopyToWorkingDirectory(new DirectoryInfo(MyConfig.AdditionalContentResourceDirectory));

			// Write the external files (FilesToInclude) to the html output directory

			foreach (string srcFilePattern in MyConfig.FilesToInclude.Split('|')) {
				if (string.IsNullOrEmpty(srcFilePattern))
					continue;

				string path = Path.GetDirectoryName(srcFilePattern);
				string pattern = Path.GetFileName(srcFilePattern);

				// Path.GetDirectoryName can return null in some cases.
				// Treat this as an empty string.
				if (path == null)
					path = string.Empty;

				// Make sure we have a fully-qualified path name
				if (!Path.IsPathRooted(path))
					path = Path.Combine(Environment.CurrentDirectory, path);

				// Directory.GetFiles does not accept null or empty string
				// for the searchPattern parameter. When no pattern was
				// specified, assume all files (*) are wanted.
				if (string.IsNullOrEmpty(pattern))
					pattern = "*";

				foreach (string srcFile in Directory.GetFiles(path, pattern)) {
					string dstFile = Path.Combine(buildContext.WorkingDirectory.FullName, Path.GetFileName(srcFile));
					File.Copy(srcFile, dstFile, true);
					File.SetAttributes(dstFile, FileAttributes.Archive);
					filesToInclude.Add(dstFile);
				}
			}
		}

		private static void XslTransform(BuildProjectContext buildContext, string stylesheetName, IXPathNavigable xpathNavigable, XsltArgumentList arguments, TextWriter writer, string targetFilename)
		{
			StyleSheet stylesheet = buildContext.stylesheets[stylesheetName];
			try {
				stylesheet.Transform(xpathNavigable, arguments, writer);
			} catch (XsltException ex) {
				throw new DocumenterException(string.Format("XSLT error while writing file {0} using stylesheet {1}({2}:{3}) : {4}", targetFilename, stylesheetName, ex.LineNumber, ex.LinePosition, ex.Message));
			}
		}

		private MsdnDocumenterConfig MyConfig
		{
			get
			{
				return (MsdnDocumenterConfig)Config;
			}
		}

		private static WhichType GetWhichType(XmlNode typeNode)
		{
			WhichType whichType;

			switch (typeNode.Name) {
				case "class":
					whichType = WhichType.Class;
					break;
				case "interface":
					whichType = WhichType.Interface;
					break;
				case "structure":
					whichType = WhichType.Structure;
					break;
				case "enumeration":
					whichType = WhichType.Enumeration;
					break;
				case "delegate":
					whichType = WhichType.Delegate;
					break;
				default:
					whichType = WhichType.Unknown;
					break;
			}

			return whichType;
		}

		private void MakeHtmlForAssemblies(BuildProjectContext ctx, bool mergeAssemblies)
		{
#if DEBUG
			int start = Environment.TickCount;
#endif

			MakeHtmlForAssembliesSorted(ctx, mergeAssemblies);

#if DEBUG
			Trace.WriteLine("Making Html: " + ((Environment.TickCount - start) / 1000.0) + " sec.");
#endif
		}

		private void MakeHtmlForAssembliesSorted(BuildProjectContext ctx, bool mergeAssemblies)
		{
			const string defaultNamespace = null;

			XmlNodeList assemblyNodes = ctx.SelectNodes("/ndoc:ndoc/ndoc:assembly");

			List<string> assemblyNames = new List<string>();
			foreach(XmlNode node in assemblyNodes) assemblyNames.Add(GetNodeName(node));
			assemblyNames.Sort();

			if (mergeAssemblies)
            {
                // sort namespaces alphabetically except for defaultNamespace, which is always first
				string[] namespaces = SortNamespaces(ctx, assemblyNames, defaultNamespace);
				MakeHtmlForNamespaces(ctx, null, namespaces);                
            }
            else
            {
                foreach (string currentAssemblyName in assemblyNames)
                {
                    MakeHtmlForAssembly(ctx, currentAssemblyName);

					ctx.htmlHelp.OpenBookInContents();
                    string[] namespaces = SortNamespaces(ctx, new List<string>( new[] { currentAssemblyName }) , defaultNamespace);
					MakeHtmlForNamespaces(ctx, currentAssemblyName, namespaces);
					ctx.htmlHelp.CloseBookInContents();
                }
            }
        }

		private void MakeHtmlForAssembly(BuildProjectContext ctx, string assemblyName)
		{
			BuildAssemblyContext actx = new BuildAssemblyContext(ctx, assemblyName);
			string fileName = ctx._nameResolver.GetFilenameForAssembly(assemblyName);

			XsltArgumentList arguments = new XsltArgumentList();
			arguments.AddParam("assembly-name", String.Empty, assemblyName);
			TransformAndWriteResult(actx, "assembly", arguments, fileName);

			ctx.htmlHelp.AddFileToContents(assemblyName + " Assembly", fileName, HtmlHelpIcon.Page);
		}

		private void MakeHtmlForNamespaces(BuildProjectContext ctx, string currentAssembly, IList<string> namespaces)
		{
			int nNodes = namespaces.Count;

			bool heirTOC = (MyConfig.NamespaceTOCStyle == TOCStyle.Hierarchical);
			int level = 0;

			string[] last = new string[0];

			BuildAssemblyContext generatorContext = null;
			for (int i = 0; i < nNodes; i++) {
				OnDocBuildingProgress(i * 100 / nNodes); // TODO (EE): fix calc for !MergeAssemblies mode

				string currentNamespace = namespaces[i];
				// determine assembly containing this namespace
				XmlNodeList namespaceNodes = (currentAssembly==null) 
					? ctx.SelectNodes(string.Format("/ndoc:ndoc/ndoc:assembly/ndoc:module/ndoc:namespace[@name='{0}']", currentNamespace))
					: ctx.SelectNodes(string.Format("/ndoc:ndoc/ndoc:assembly[@name='{0}']/ndoc:module/ndoc:namespace[@name='{1}']", currentAssembly, currentNamespace));

				string assemblyName = GetNodeName(ctx.SelectSingleNode(namespaceNodes[0], "ancestor::ndoc:assembly"));
				generatorContext = new BuildAssemblyContext(ctx, assemblyName);

				if (heirTOC) {
					string[] split = currentNamespace.Split('.');

					for (level = last.Length; level >= 0 &&
						ArrayEquals(split, 0, last, 0, level) == false; level--) {
						if (level > last.Length)
							continue;

						string namespaceName = string.Join(".", last, 0, level);
						XmlNodeList typeNodes = GetTypeNodes(ctx, currentAssembly, namespaceName);
						MakeHtmlForTypes(generatorContext, typeNodes);
						ctx.htmlHelp.CloseBookInContents();
					}

					if (level < 0)
						level = 0;

					for (; level < split.Length; level++) {
						string namespaceName = string.Join(".", split, 0, level + 1);

						if (!namespaces.Contains(namespaceName))
//						if (Array.BinarySearch(namespaces, namespaceName) < 0)
							MakeHtmlForNamespace(generatorContext, split[level], namespaceName, false);
						else
							MakeHtmlForNamespace(generatorContext, split[level], namespaceName, true);

						ctx.htmlHelp.OpenBookInContents();
					}

					last = split;
				} else {
					MakeHtmlForNamespace(generatorContext, currentNamespace, currentNamespace, true);
					using (ctx.htmlHelp.OpenBookInContents()) {
						XmlNodeList typeNodes = GetTypeNodes(ctx, currentAssembly, currentNamespace);
						MakeHtmlForTypes(generatorContext, typeNodes);
					}
				}
			}


			if (heirTOC && last.Length > 0) {
				for (; level >= 1; level--) {
					string ns = string.Join(".", last, 0, level);
					XmlNodeList typeNodes = GetTypeNodes(ctx, currentAssembly, ns);
					MakeHtmlForTypes(generatorContext, typeNodes);
					ctx.htmlHelp.CloseBookInContents();
				}
			}

			OnDocBuildingProgress(100);
		}

		private static XmlNodeList GetTypeNodes(BuildProjectContext ctx, string assembly, string namespaceName)
		{
			string xpath = (assembly == null)
			               	? string.Format(
			               	  	"/ndoc:ndoc/ndoc:assembly/ndoc:module/ndoc:namespace[@name='{0}']/*[local-name()!='documentation' and local-name()!='typeHierarchy']",
			               	  	namespaceName)
			               	: string.Format(
			               	  	"/ndoc:ndoc/ndoc:assembly[@name='{0}']/ndoc:module/ndoc:namespace[@name='{1}']/*[local-name()!='documentation' and local-name()!='typeHierarchy']",
								assembly,
			               	  	namespaceName);
			XmlNodeList typeNodes = ctx.SelectNodes(xpath);
			return typeNodes;			
		}

		private static bool ArrayEquals(string[] array1, int from1, string[] array2, int from2, int count)
		{
			for (int i = 0; i < count; i++) {
				if (array1[from1 + i] != array2[from2 + i])
					return false;
			}

			return true;
		}

		private static void GetNamespacesFromAssembly(BuildProjectContext buildContext, string assemblyName, NameValueCollection namespaceAssemblies)
		{
			XmlNodeList namespaceNodes = buildContext.SelectNodes(string.Format("/ndoc:ndoc/ndoc:assembly[@name='{0}']/ndoc:module/ndoc:namespace", assemblyName));
			foreach (XmlNode namespaceNode in namespaceNodes) {
				string namespaceName = GetNodeName(namespaceNode);
				namespaceAssemblies.Add(namespaceName, assemblyName);
			}
		}

		/// <summary>
		/// Add the namespace elements to the output
		/// </summary>
		/// <remarks>
		/// The namespace 
		/// </remarks>
		/// <param name="ctx"></param>
		/// <param name="namespacePart">If nested, the namespace part will be the current
		/// namespace element being documented</param>
		/// <param name="namespaceName">The full namespace name being documented</param>
		/// <param name="addDocumentation">If true, the namespace will be documented, if false
		/// the node in the TOC will not link to a page</param>
		private void MakeHtmlForNamespace(BuildAssemblyContext ctx, string namespacePart, string namespaceName,
			bool addDocumentation)
		{
			//			// handle duplicate namespace documentation
			//			if (ctx.documentedNamespaces.Contains(namespaceName)) 
			//				return;
			//			ctx.documentedNamespaces.Add(namespaceName);

			if (addDocumentation) {
				string currentAssemblyName = (ctx.MergeAssemblies) ? string.Empty : ctx.CurrentAssemblyName;

				string namespaceFilename = ctx._nameResolver.GetFilenameForNamespace(currentAssemblyName, namespaceName);

				ctx.htmlHelp.AddFileToContents(namespacePart, namespaceFilename);

				XsltArgumentList arguments = new XsltArgumentList();
				arguments.AddParam("merge-assemblies", String.Empty, ctx.MergeAssemblies);
				arguments.AddParam("namespace", String.Empty, namespaceName);
				TransformAndWriteResult(ctx, "namespace", arguments, namespaceFilename);

				string namespaceHierarchyFilename = ctx._nameResolver.GetFilenameForNamespaceHierarchy(currentAssemblyName, namespaceName);
				arguments = new XsltArgumentList();
				arguments.AddParam("merge-assemblies", String.Empty, ctx.MergeAssemblies);
				arguments.AddParam("namespace", String.Empty, namespaceName);
				TransformAndWriteResult(ctx, "namespacehierarchy", arguments, namespaceHierarchyFilename);
			} else {
				ctx.htmlHelp.AddFileToContents(namespacePart);
			}
		}

		private void MakeHtmlForTypes(BuildProjectContext projectCtx, XmlNodeList typeNodes)
		{
			int[] indexes = SortNodesByAttribute(typeNodes, "id");
			int nNodes = typeNodes.Count;

			for (int i = 0; i < nNodes; i++) {
				XmlNode typeNode = typeNodes[indexes[i]];
				WhichType whichType = GetWhichType(typeNode);

				string assemblyName = XmlUtils.GetNodeName(projectCtx.SelectSingleNode(typeNode, "ancestor::ndoc:assembly"));
				BuildAssemblyContext ctx = new BuildAssemblyContext(projectCtx, assemblyName); // TODO (EE): initialize w/ assembly name

				switch (whichType) {
					case WhichType.Class:
						MakeHtmlForInterfaceOrClassOrStructure(ctx, whichType, typeNode);
						break;
					case WhichType.Interface:
						MakeHtmlForInterfaceOrClassOrStructure(ctx, whichType, typeNode);
						break;
					case WhichType.Structure:
						MakeHtmlForInterfaceOrClassOrStructure(ctx, whichType, typeNode);
						break;
					case WhichType.Enumeration:
						MakeHtmlForEnumerationOrDelegate(ctx, whichType, typeNode);
						break;
					case WhichType.Delegate:
						MakeHtmlForEnumerationOrDelegate(ctx, whichType, typeNode);
						break;
					default:
						break;
				}
			}
		}

		private void MakeHtmlForEnumerationOrDelegate(BuildAssemblyContext ctx, WhichType whichType, XmlNode typeNode)
		{
			string typeName = whichType == WhichType.Delegate ? GetNodeDisplayName(typeNode) : GetNodeName(typeNode);
			string typeID = GetNodeId(typeNode);
			string fileName = ctx._nameResolver.GetFilenameForId(ctx.CurrentAssemblyName, typeID);

			ctx.htmlHelp.AddFileToContents(typeName + " " + mixedCaseTypeNames[whichType], fileName, HtmlHelpIcon.Page);

			XsltArgumentList arguments = new XsltArgumentList();
			arguments.AddParam("type-id", String.Empty, typeID);
			TransformAndWriteResult(ctx, "type", arguments, fileName);
		}

		private void MakeHtmlForInterfaceOrClassOrStructure(BuildAssemblyContext ctx,
			WhichType whichType,
			XmlNode typeNode)
		{
			string typeName = GetNodeDisplayName(typeNode);
			string typeID = GetNodeId(typeNode);
			string fileName = ctx._nameResolver.GetFilenameForId(ctx.CurrentAssemblyName, typeID);

			ctx.htmlHelp.AddFileToContents(typeName + " " + mixedCaseTypeNames[whichType], fileName);

			bool hasMembers = ctx.SelectNodes(typeNode, "ndoc:constructor|ndoc:field|ndoc:property|ndoc:method|ndoc:operator|ndoc:event").Count > 0;

			if (hasMembers) {
				ctx.htmlHelp.OpenBookInContents();
			}

			XsltArgumentList arguments = new XsltArgumentList();
			arguments.AddParam("type-id", String.Empty, typeID);
			TransformAndWriteResult(ctx, "type", arguments, fileName);

			if (ctx.SelectNodes(typeNode, "ndoc:derivedBy").Count > 5) {
				fileName = ctx._nameResolver.GetFilenameForTypeHierarchy(ctx.CurrentAssemblyName, typeID);
				arguments = new XsltArgumentList();
				arguments.AddParam("type-id", String.Empty, typeID);
				TransformAndWriteResult(ctx, "typehierarchy", arguments, fileName);
			}

			if (hasMembers) {
				fileName = ctx._nameResolver.GetFilenameForTypeMemberList(ctx.CurrentAssemblyName, typeID);
				ctx.htmlHelp.AddFileToContents(typeName + " Members",
					fileName,
					HtmlHelpIcon.Page);

				arguments = new XsltArgumentList();
				arguments.AddParam("type-id", String.Empty, typeID);
				TransformAndWriteResult(ctx, "allmembers", arguments, fileName);

				MakeHtmlForConstructors(ctx, typeNode);
				MakeHtmlForFields(ctx, typeNode);
				MakeHtmlForProperties(ctx, typeNode);
				MakeHtmlForMethods(ctx, typeNode);
				MakeHtmlForOperators(ctx, typeNode);
				MakeHtmlForEvents(ctx, typeNode);

				ctx.htmlHelp.CloseBookInContents();
			}
		}

		private void MakeHtmlForConstructors(BuildAssemblyContext ctx, XmlNode typeNode)
		{
			string constructorID;
			string fileName;

			string typeName = GetNodeDisplayName(typeNode);
			string typeID = GetNodeId(typeNode);

			XmlNodeList constructorNodes = ctx.SelectNodes(typeNode, "ndoc:constructor[@contract!='Static']");
			// If the constructor is overloaded then make an overload page.
			if (constructorNodes.Count > 1) {
				fileName = ctx._nameResolver.GetFilenameForConstructorList(ctx.CurrentAssemblyName, typeID);
				ctx.htmlHelp.AddFileToContents(typeName + " Constructor", fileName);

				ctx.htmlHelp.OpenBookInContents();

				constructorID = constructorNodes[0].Attributes["id"].Value;

				XsltArgumentList arguments = new XsltArgumentList();
				arguments.AddParam("member-id", String.Empty, constructorID);
				TransformAndWriteResult(ctx, "memberoverload", arguments, fileName);
			}

			foreach (XmlNode constructorNode in constructorNodes) {
				constructorID = constructorNode.Attributes["id"].Value;
				fileName = ctx._nameResolver.GetFilenameForId(ctx.CurrentAssemblyName, constructorID);

				if (constructorNodes.Count > 1) {
					XmlNodeList parameterNodes = ctx.SelectNodes(constructorNode, "ndoc:parameter");
					ctx.htmlHelp.AddFileToContents(typeName + " Constructor " + GetParamList(parameterNodes), fileName,
						HtmlHelpIcon.Page);
				} else {
					ctx.htmlHelp.AddFileToContents(typeName + " Constructor", fileName, HtmlHelpIcon.Page);
				}

				XsltArgumentList arguments = new XsltArgumentList();
				arguments.AddParam("member-id", String.Empty, constructorID);
				TransformAndWriteResult(ctx, "member", arguments, fileName);
			}

			if (constructorNodes.Count > 1) {
				ctx.htmlHelp.CloseBookInContents();
			}

			XmlNode staticConstructorNode = ctx.SelectSingleNode(typeNode, "ndoc:constructor[@contract='Static']");
			if (staticConstructorNode != null) {
				constructorID = staticConstructorNode.Attributes["id"].Value;
				fileName = ctx._nameResolver.GetFilenameForId(ctx.CurrentAssemblyName, constructorID);

				ctx.htmlHelp.AddFileToContents(typeName + " Static Constructor", fileName, HtmlHelpIcon.Page);

				XsltArgumentList arguments = new XsltArgumentList();
				arguments.AddParam("member-id", String.Empty, constructorID);
				TransformAndWriteResult(ctx, "member", arguments, fileName);
			}
		}

		private void MakeHtmlForFields(BuildAssemblyContext ctx, XmlNode typeNode)
		{
			XmlNodeList fields = ctx.SelectNodes(typeNode, "ndoc:field[not(@declaringType)]");

			if (fields.Count > 0) {
				//string typeName = typeNode.Attributes["name"].Value;
				string typeID = GetNodeId(typeNode);
				string fileName = ctx._nameResolver.GetFilenameForFieldList(ctx.CurrentAssemblyName, typeID);

				ctx.htmlHelp.AddFileToContents("Fields", fileName);

				XsltArgumentList arguments = new XsltArgumentList();
				arguments.AddParam("type-id", String.Empty, typeID);
				arguments.AddParam("member-type", String.Empty, "field");
				TransformAndWriteResult(ctx, "individualmembers", arguments, fileName);

				ctx.htmlHelp.OpenBookInContents();

				int[] indexes = SortNodesByAttribute(fields, "id");

				foreach (int index in indexes) {
					XmlNode field = fields[index];

					string fieldName = GetNodeName(field);
					string fieldID = GetNodeId(field);
					fileName = ctx._nameResolver.GetFilenameForId(ctx.CurrentAssemblyName, fieldID);
					ctx.htmlHelp.AddFileToContents(fieldName + " Field", fileName, HtmlHelpIcon.Page);

					arguments = new XsltArgumentList();
					arguments.AddParam("field-id", String.Empty, fieldID);
					TransformAndWriteResult(ctx, "field", arguments, fileName);
				}

				ctx.htmlHelp.CloseBookInContents();
			}
		}

		private void MakeHtmlForProperties(BuildAssemblyContext ctx, XmlNode typeNode)
		{
			XmlNodeList declaredPropertyNodes = ctx.SelectNodes(typeNode, "ndoc:property[not(@declaringType)]");

			if (declaredPropertyNodes.Count > 0) {
				XmlNode propertyNode;
				bool bOverloaded = false;
				int i;

				string typeID = GetNodeId(typeNode);
				XmlNodeList propertyNodes = ctx.SelectNodes(typeNode, "ndoc:property[not(@declaringType)]");
				int nNodes = propertyNodes.Count;

				int[] indexes = SortNodesByAttribute(propertyNodes, "id");

				string fileName = ctx._nameResolver.GetFilenameForPropertyList(ctx.CurrentAssemblyName, typeID);
				ctx.htmlHelp.AddFileToContents("Properties", fileName);

				XsltArgumentList arguments = new XsltArgumentList();
				arguments.AddParam("type-id", String.Empty, typeID);
				arguments.AddParam("member-type", String.Empty, "property");
				TransformAndWriteResult(ctx, "individualmembers", arguments, fileName);

				ctx.htmlHelp.OpenBookInContents();

				for (i = 0; i < nNodes; i++) {
					propertyNode = propertyNodes[indexes[i]];

					string propertyName = propertyNode.Attributes["name"].Value;
					string propertyID = propertyNode.Attributes["id"].Value;

					// If the method is overloaded then make an overload page.
					string previousPropertyName = ((i - 1 < 0) || (propertyNodes[indexes[i - 1]].Attributes.Count == 0))
													? "" : propertyNodes[indexes[i - 1]].Attributes[0].Value;
					string nextPropertyName = ((i + 1 == nNodes) || (propertyNodes[indexes[i + 1]].Attributes.Count == 0))
												? "" : propertyNodes[indexes[i + 1]].Attributes[0].Value;

					if ((previousPropertyName != propertyName) && (nextPropertyName == propertyName)) {
						fileName = ctx._nameResolver.GetFilenameForPropertyOverloads(ctx.CurrentAssemblyName, typeID, propertyName);
						ctx.htmlHelp.AddFileToContents(propertyName + " Property", fileName);

						arguments = new XsltArgumentList();
						arguments.AddParam("member-id", String.Empty, propertyID);
						TransformAndWriteResult(ctx, "memberoverload", arguments, fileName);

						ctx.htmlHelp.OpenBookInContents();

						bOverloaded = true;
					}

					fileName = ctx._nameResolver.GetFilenameForId(ctx.CurrentAssemblyName, propertyID);

					string pageTitle;
					if (!bOverloaded) {
						pageTitle = string.Format("{0} Property", propertyName);
					} else {
						XmlNodeList parameterNodes = ctx.SelectNodes(propertyNode, "ns:parameter");
						pageTitle = string.Format("{0} Property {1}", propertyName, GetParamList(parameterNodes));
					}
					ctx.htmlHelp.AddFileToContents(pageTitle, fileName, HtmlHelpIcon.Page);

					XsltArgumentList arguments2 = new XsltArgumentList();
					arguments2.AddParam("property-id", String.Empty, propertyID);
					TransformAndWriteResult(ctx, "property", arguments2, fileName);

					if ((previousPropertyName == propertyName) && (nextPropertyName != propertyName)) {
						ctx.htmlHelp.CloseBookInContents();
						bOverloaded = false;
					}
				}

				ctx.htmlHelp.CloseBookInContents();
			}
		}

		private static string GetPreviousMethodName(XmlNodeList methodNodes, int[] indexes, int index)
		{
			while (--index >= 0) {
				if (methodNodes[indexes[index]].Attributes["declaringType"] == null)
					return methodNodes[indexes[index]].Attributes["name"].Value;
			}
			return null;
		}

		private static string GetNextMethodName(XmlNodeList methodNodes, int[] indexes, int index)
		{
			while (++index < methodNodes.Count) {
				if (methodNodes[indexes[index]].Attributes["declaringType"] == null)
					return methodNodes[indexes[index]].Attributes["name"].Value;
			}
			return null;
		}

		// returns true, if method is neither overload of a method in the same class,
		// nor overload of a method in the base class.
		private static bool IsMethodAlone(XmlNodeList methodNodes, int[] indexes, int index)
		{
			string name = methodNodes[indexes[index]].Attributes["name"].Value;
			int lastIndex = methodNodes.Count - 1;
			if (lastIndex <= 0)
				return true;
			bool previousNameDifferent = (index == 0)
				|| (methodNodes[indexes[index - 1]].Attributes["name"].Value != name);
			bool nextNameDifferent = (index == lastIndex)
				|| (methodNodes[indexes[index + 1]].Attributes["name"].Value != name);
			return (previousNameDifferent && nextNameDifferent);
		}

		private static bool IsMethodFirstOverload(XmlNodeList methodNodes, int[] indexes, int index)
		{
			if ((methodNodes[indexes[index]].Attributes["declaringType"] != null)
				|| IsMethodAlone(methodNodes, indexes, index))
				return false;

			string name = methodNodes[indexes[index]].Attributes["name"].Value;
			string previousName = GetPreviousMethodName(methodNodes, indexes, index);
			return previousName != name;
		}

		private static bool IsMethodLastOverload(XmlNodeList methodNodes, int[] indexes, int index)
		{
			if ((methodNodes[indexes[index]].Attributes["declaringType"] != null)
				|| IsMethodAlone(methodNodes, indexes, index))
				return false;

			string name = methodNodes[indexes[index]].Attributes["name"].Value;
			string nextName = GetNextMethodName(methodNodes, indexes, index);
			return nextName != name;
		}

		private void MakeHtmlForMethods(BuildAssemblyContext ctx, XmlNode typeNode)
		{
			XmlNodeList declaredMethodNodes = ctx.SelectNodes(typeNode, "ndoc:method[not(@declaringType)]");

			if (declaredMethodNodes.Count > 0) {
				bool bOverloaded = false;

				string typeID = GetNodeId(typeNode);
				XmlNodeList methodNodes = ctx.SelectNodes(typeNode, "ndoc:method");
				int nNodes = methodNodes.Count;

				int[] indexes = SortNodesByAttribute(methodNodes, "id");

				string fileName = ctx._nameResolver.GetFilenameForMethodList(ctx.CurrentAssemblyName, typeID);
				ctx.htmlHelp.AddFileToContents("Methods", fileName);

				XsltArgumentList arguments = new XsltArgumentList();
				arguments.AddParam("type-id", String.Empty, typeID);
				arguments.AddParam("member-type", String.Empty, "method");
				TransformAndWriteResult(ctx, "individualmembers", arguments, fileName);

				ctx.htmlHelp.OpenBookInContents();

				for (int i = 0; i < nNodes; i++) {
					XmlNode methodNode = methodNodes[indexes[i]];
					string methodDisplayName = GetNodeDisplayName(methodNode);
					string methodName = GetNodeName(methodNode);
					string methodID = GetNodeId(methodNode);

					if (IsMethodFirstOverload(methodNodes, indexes, i)) {
						bOverloaded = true;

						fileName = ctx._nameResolver.GetFilenameForMethodOverloads(ctx.CurrentAssemblyName, typeID, methodName);
						ctx.htmlHelp.AddFileToContents(methodDisplayName + " Method", fileName);

						arguments = new XsltArgumentList();
						arguments.AddParam("member-id", String.Empty, methodID);
						TransformAndWriteResult(ctx, "memberoverload", arguments, fileName);

						ctx.htmlHelp.OpenBookInContents();
					}

					if (XmlUtils.GetAttributeString(methodNode, "declaringType", false) == null) {
						fileName = ctx._nameResolver.GetFilenameForId(ctx.CurrentAssemblyName, methodID);

						string pageTitle;
						if (bOverloaded) {
							XmlNodeList parameterNodes = ctx.SelectNodes(methodNode, "ndoc:parameter");
							pageTitle = methodDisplayName + GetParamList(parameterNodes) + " Method ";
						} else {
							pageTitle = methodDisplayName + " Method";
						}
						ctx.htmlHelp.AddFileToContents(pageTitle, fileName,
							HtmlHelpIcon.Page);

						XsltArgumentList arguments2 = new XsltArgumentList();
						arguments2.AddParam("member-id", String.Empty, methodID);
						TransformAndWriteResult(ctx, "member", arguments2, fileName);
					}

					if (bOverloaded && IsMethodLastOverload(methodNodes, indexes, i)) {
						bOverloaded = false;
						ctx.htmlHelp.CloseBookInContents();
					}
				}

				ctx.htmlHelp.CloseBookInContents();
			}
		}

		private void MakeHtmlForOperators(BuildAssemblyContext ctx, XmlNode typeNode)
		{
			XmlNodeList opNodes = ctx.SelectNodes(typeNode, "ndoc:operator");

			if (opNodes.Count == 0)
				return;

			string typeID = GetNodeId(typeNode);
			string fileName = ctx._nameResolver.GetFilenameForOperatorList(ctx.CurrentAssemblyName, typeID);
			bool bOverloaded = false;

			bool bHasOperators =
				(ctx.SelectSingleNode(typeNode, "ndoc:operator[@name != 'op_Explicit' and @name != 'op_Implicit']") != null);
			bool bHasConverters =
				(ctx.SelectSingleNode(typeNode, "ndoc:operator[@name  = 'op_Explicit' or  @name  = 'op_Implicit']") != null);
			string pageTitle = "";

			if (bHasOperators) {
				pageTitle = bHasConverters ? "Operators and Type Conversions" : "Operators";
			} else {
				if (bHasConverters) {
					pageTitle = "Type Conversions";
				}
			}

			ctx.htmlHelp.AddFileToContents(pageTitle, fileName);

			XsltArgumentList arguments = new XsltArgumentList();
			arguments.AddParam("type-id", String.Empty, typeID);
			arguments.AddParam("member-type", String.Empty, "operator");
			TransformAndWriteResult(ctx, "individualmembers", arguments, fileName);

			ctx.htmlHelp.OpenBookInContents();

			int[] indexes = SortNodesByAttribute(opNodes, "id");
			int nNodes = opNodes.Count;

			//operators first
			for (int i = 0; i < nNodes; i++) {
				XmlNode operatorNode = opNodes[indexes[i]];

				string operatorID = GetNodeId(operatorNode);
				string opName = GetNodeName(operatorNode);
				if ((opName != "op_Implicit") && (opName != "op_Explicit")) {
					if (IsMethodFirstOverload(opNodes, indexes, i)) {
						bOverloaded = true;

						fileName = ctx._nameResolver.GetFilenameForOperatorOverloads(ctx.CurrentAssemblyName, typeID, opName);
						ctx.htmlHelp.AddFileToContents(GetOperatorDisplayName(ctx, operatorNode), fileName);

						arguments = new XsltArgumentList();
						arguments.AddParam("member-id", String.Empty, operatorID);
						TransformAndWriteResult(ctx, "memberoverload", arguments, fileName);

						ctx.htmlHelp.OpenBookInContents();
					}


					fileName = ctx._nameResolver.GetFilenameForId(ctx.CurrentAssemblyName, operatorID);
					string opPageTitle;
					if (bOverloaded) {
						XmlNodeList parameterNodes = ctx.SelectNodes(operatorNode, "ns:parameter");
						opPageTitle = GetOperatorDisplayName(ctx, operatorNode) + GetParamList(parameterNodes);
					} else {
						opPageTitle = GetOperatorDisplayName(ctx, operatorNode);
					}
					ctx.htmlHelp.AddFileToContents(opPageTitle, fileName,
												   HtmlHelpIcon.Page);

					arguments = new XsltArgumentList();
					arguments.AddParam("member-id", String.Empty, operatorID);
					TransformAndWriteResult(ctx, "member", arguments, fileName);

					if (bOverloaded && IsMethodLastOverload(opNodes, indexes, i)) {
						bOverloaded = false;
						ctx.htmlHelp.CloseBookInContents();
					}
				}
			}

			//type converters
			for (int i = 0; i < nNodes; i++) {
				XmlNode operatorNode = opNodes[indexes[i]];
				string operatorID = GetNodeId(operatorNode);
				string opName = GetNodeName(operatorNode);

				if ((opName == "op_Implicit") || (opName == "op_Explicit")) {
					fileName = ctx._nameResolver.GetFilenameForId(ctx.CurrentAssemblyName, operatorID);
					ctx.htmlHelp.AddFileToContents(GetOperatorDisplayName(ctx, operatorNode), fileName,
												   HtmlHelpIcon.Page);

					arguments = new XsltArgumentList();
					arguments.AddParam("member-id", String.Empty, operatorID);
					TransformAndWriteResult(ctx, "member", arguments, fileName);
				}
			}

			ctx.htmlHelp.CloseBookInContents();
		}

		private static string GetOperatorDisplayName(BuildProjectContext ctx, XmlNode operatorNode)
		{
			string name = GetNodeName(operatorNode);

			switch (name) {
				case "op_Decrement":
					return "Decrement Operator";
				case "op_Increment":
					return "Increment Operator";
				case "op_UnaryNegation":
					return "Unary Negation Operator";
				case "op_UnaryPlus":
					return "Unary Plus Operator";
				case "op_LogicalNot":
					return "Logical Not Operator";
				case "op_True":
					return "True Operator";
				case "op_False":
					return "False Operator";
				case "op_AddressOf":
					return "Address Of Operator";
				case "op_OnesComplement":
					return "Ones Complement Operator";
				case "op_PointerDereference":
					return "Pointer Dereference Operator";
				case "op_Addition":
					return "Addition Operator";
				case "op_Subtraction":
					return "Subtraction Operator";
				case "op_Multiply":
					return "Multiplication Operator";
				case "op_Division":
					return "Division Operator";
				case "op_Modulus":
					return "Modulus Operator";
				case "op_ExclusiveOr":
					return "Exclusive Or Operator";
				case "op_BitwiseAnd":
					return "Bitwise And Operator";
				case "op_BitwiseOr":
					return "Bitwise Or Operator";
				case "op_LogicalAnd":
					return "LogicalAnd Operator";
				case "op_LogicalOr":
					return "Logical Or Operator";
				case "op_Assign":
					return "Assignment Operator";
				case "op_LeftShift":
					return "Left Shift Operator";
				case "op_RightShift":
					return "Right Shift Operator";
				case "op_SignedRightShift":
					return "Signed Right Shift Operator";
				case "op_UnsignedRightShift":
					return "Unsigned Right Shift Operator";
				case "op_Equality":
					return "Equality Operator";
				case "op_GreaterThan":
					return "Greater Than Operator";
				case "op_LessThan":
					return "Less Than Operator";
				case "op_Inequality":
					return "Inequality Operator";
				case "op_GreaterThanOrEqual":
					return "Greater Than Or Equal Operator";
				case "op_LessThanOrEqual":
					return "Less Than Or Equal Operator";
				case "op_UnsignedRightShiftAssignment":
					return "Unsigned Right Shift Assignment Operator";
				case "op_MemberSelection":
					return "Member Selection Operator";
				case "op_RightShiftAssignment":
					return "Right Shift Assignment Operator";
				case "op_MultiplicationAssignment":
					return "Multiplication Assignment Operator";
				case "op_PointerToMemberSelection":
					return "Pointer To Member Selection Operator";
				case "op_SubtractionAssignment":
					return "Subtraction Assignment Operator";
				case "op_ExclusiveOrAssignment":
					return "Exclusive Or Assignment Operator";
				case "op_LeftShiftAssignment":
					return "Left Shift Assignment Operator";
				case "op_ModulusAssignment":
					return "Modulus Assignment Operator";
				case "op_AdditionAssignment":
					return "Addition Assignment Operator";
				case "op_BitwiseAndAssignment":
					return "Bitwise And Assignment Operator";
				case "op_BitwiseOrAssignment":
					return "Bitwise Or Assignment Operator";
				case "op_Comma":
					return "Comma Operator";
				case "op_DivisionAssignment":
					return "Division Assignment Operator";
				case "op_Explicit": {
						XmlNode parameterNode = ctx.SelectSingleNode(operatorNode, "ndoc:parameter");
						string from = GetNodeTypeId(parameterNode);
						string to = GetNodeTypeId(ctx.SelectSingleNode(operatorNode, "ndoc:returnType"));
						return "Explicit " + StripNamespace(from) + " to " + StripNamespace(to) + " Conversion";
					}
				case "op_Implicit": {
						XmlNode parameterNode = ctx.SelectSingleNode(operatorNode, "ndoc:parameter");
						string from = GetNodeTypeId(parameterNode);
						string to = GetNodeTypeId(ctx.SelectSingleNode(operatorNode, "ndoc:returnType"));
						return "Implicit " + StripNamespace(from) + " to " + StripNamespace(to) + " Conversion";
					}
				default:
					return "ERROR";
			}
		}

		private void MakeHtmlForEvents(BuildAssemblyContext ctx, XmlNode typeNode)
		{
			XmlNodeList declaredEventNodes = ctx.SelectNodes(typeNode, "ndoc:event[not(@declaringType)]");

			if (declaredEventNodes.Count > 0) {
				XmlNodeList events = ctx.SelectNodes(typeNode, "ns:event");

				if (events.Count > 0) {
					//string typeName = (string)typeNode.Attributes["name"].Value;
					string typeID = GetNodeId(typeNode);
					string fileName = ctx._nameResolver.GetFilenameForEventList(ctx.CurrentAssemblyName, typeID);

					ctx.htmlHelp.AddFileToContents("Events", fileName);

					XsltArgumentList arguments = new XsltArgumentList();
					arguments.AddParam("type-id", String.Empty, typeID);
					arguments.AddParam("member-type", String.Empty, "event");
					TransformAndWriteResult(ctx, "individualmembers", arguments, fileName);

					ctx.htmlHelp.OpenBookInContents();

					int[] indexes = SortNodesByAttribute(events, "id");

					foreach (int index in indexes) {
						XmlNode eventElement = events[index];

						if (XmlUtils.GetAttributeString(eventElement, "declaringType", false) == null) {
							string eventName = GetNodeName(eventElement);
							string eventID = GetNodeId(eventElement);

							fileName = ctx._nameResolver.GetFilenameForId(ctx.CurrentAssemblyName, eventID);
							ctx.htmlHelp.AddFileToContents(eventName + " Event",
								fileName,
								HtmlHelpIcon.Page);

							arguments = new XsltArgumentList();
							arguments.AddParam("event-id", String.Empty, eventID);
							TransformAndWriteResult(ctx, "event", arguments, fileName);
						}
					}

					ctx.htmlHelp.CloseBookInContents();
				}
			}
		}

		private static string GetParamList(XmlNodeList parameterNodes)
		{
			ArrayList parameters = new ArrayList();

			foreach (XmlNode parameterNode in parameterNodes) {

				string parameterTypeName = GetParameterTypeName(parameterNode, "displayName");
				
				parameters.Add(parameterTypeName);
			}

			string[] parameterTypeNames = (string[]) parameters.ToArray(typeof (string));
			string paramList = "(" + string.Join(",", parameterTypeNames) + ")";

			return paramList;
		}

		private static string GetParameterTypeName(XmlNode root, string typeAttributeName)
		{
			XmlAttribute typeAtt = root.Attributes[typeAttributeName];
			return typeAtt.Value;
		}

		private static string GetNodeId(XmlNode node)
		{
			return XmlUtils.GetNodeId(node);
		}

		private static string GetNodeTypeId(XmlNode node)
		{
			return XmlUtils.GetNodeTypeId(node);
		}

		private static string GetNodeName(XmlNode node)
		{
			return XmlUtils.GetNodeName(node);
		}

		private static string GetNodeDisplayName(XmlNode node)
		{
			return XmlUtils.GetNodeDisplayName(node);
		}

		private static string StripNamespace(string name)
		{
			string result = name;

			int lastDot = name.LastIndexOf('.');

			if (lastDot != -1) {
				result = name.Substring(lastDot + 1);
			}

			return result;
		}

		private static int[] SortNodesByAttribute(XmlNodeList nodes, string attributeName)
		{
			int length = nodes.Count;
			string[] names = new string[length];
			int[] indexes = new int[length];
			int i = 0;

			foreach (XmlNode node in nodes) {
				names[i] = node.Attributes[attributeName].Value;
				indexes[i] = i++;
			}

			Array.Sort(names, indexes);

			return indexes;
		}

		private static string[] SortNamespaces(BuildProjectContext ctx, IList<string> assemblyNames, string defaultNamespace)
		{
			NameValueCollection namespaceAssemblies = new NameValueCollection();
			int nNodes = assemblyNames.Count;
			for (int i = 0; i < nNodes; i++) {
				string assemblyName = assemblyNames[i];
				GetNamespacesFromAssembly(ctx, assemblyName, namespaceAssemblies);
			}

			string[] namespaces = namespaceAssemblies.AllKeys;
			if (string.IsNullOrEmpty(defaultNamespace)) {
				Array.Sort(namespaces);
			} else {
				Array.Sort(namespaces, (x, y) =>
				{
					if (x == y) {
						return 0;
					} else if (x == null || x == defaultNamespace) {
						return -1;
					} else if (y == defaultNamespace) {
						return 1;
					}
					return x.CompareTo(y);
				});
			}
			return namespaces;
		}

		private void TransformAndWriteResult(BuildAssemblyContext ctx,
			string transformName,
			XsltArgumentList arguments,
			string filename)
		{
			Trace.WriteLine(filename);
#if DEBUG
			int start = Environment.TickCount;
#endif

			ExternalHtmlProvider htmlProvider = new ExternalHtmlProvider(MyConfig, filename);

			try {

				StreamWriter streamWriter;
				string fullPath = Path.Combine(ctx.WorkingDirectory.FullName, filename);
				using (streamWriter = new StreamWriter(
					File.Open(fullPath, FileMode.Create),
					ctx.CurrentFileEncoding)) {
					string DocLangCode = Enum.GetName(typeof(SdkLanguage), MyConfig.SdkDocLanguage).Replace("_", "-");
					
					MsdnXsltUtilities utilities = new MsdnXsltUtilities(ctx._nameResolver, ctx.CurrentAssemblyName, MyConfig.SdkDocVersionString, DocLangCode, MyConfig.SdkLinksOnWeb, ctx.CurrentFileEncoding);

					if (arguments.GetParam("assembly-name", string.Empty) == null) {
						arguments.AddParam("assembly-name", String.Empty, ctx.CurrentAssemblyName);
					}
					arguments.AddParam("ndoc-title", String.Empty, MyConfig.Title);
					arguments.AddParam("ndoc-vb-syntax", String.Empty, MyConfig.ShowVisualBasic);
					arguments.AddParam("ndoc-omit-object-tags", String.Empty, ((MyConfig.OutputTarget & OutputType.HtmlHelp) == 0));
					arguments.AddParam("ndoc-document-attributes", String.Empty, MyConfig.DocumentAttributes);
					arguments.AddParam("ndoc-documented-attributes", String.Empty, MyConfig.DocumentedAttributes);

					arguments.AddParam("ndoc-sdk-doc-base-url", String.Empty, utilities.SdkDocBaseUrl);
					arguments.AddParam("ndoc-sdk-doc-file-ext", String.Empty, utilities.SdkDocExt);
					arguments.AddParam("ndoc-sdk-doc-language", String.Empty, utilities.SdkDocLanguage);

					arguments.AddExtensionObject("urn:NDocUtil", utilities);
					arguments.AddExtensionObject("urn:NDocExternalHtml", htmlProvider);

					//Use new overload so we don't get obsolete warnings - clean compile :)

					XslTransform(ctx, transformName, ctx.GetXPathNavigable(), arguments, streamWriter, fullPath);
				}
			}
			catch(IOException ex)
			{
				throw new DocumenterException(string.Format("IO error while creating file {0}", filename), ex);
			}
//			catch (PathTooLongException e) {
//				throw new PathTooLongException(e.Message + "\nThe file that NDoc3 was trying to create had the following name:\n" + Path.Combine(ctx.WorkingDirectory.FullName, filename));
//			}

#if DEBUG
			Debug.WriteLine((Environment.TickCount - start) + " msec.");
#endif
			ctx.htmlHelp.AddFileToProject(filename);
		}
	}
}
