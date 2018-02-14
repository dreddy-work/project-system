﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.Packaging
{
    [Guid(PackageGuid)]
    [PackageRegistration(AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
    [ProvideAutoLoad(ActivationContextGuid)]
    [ProvideUIContextRule(ActivationContextGuid, "Load Managed Project Package",
        "dotnetcore",
        new string[] {"dotnetcore"},
        new string[] {"SolutionHasProjectCapability:(CSharp | VB) & CPS"}
        )]

    [ProvideMenuResource("Menus.ctmenu", 1)]
    internal partial class ManagedProjectSystemPackage : AsyncPackage
    {
        public const string ActivationContextGuid = "E7DF1626-44DD-4E8C-A8A0-92EAB6DDC16E";
        public const string PackageGuid = "A4F9D880-9492-4072-8BF3-2B5EEEDC9E68";
        public const string ManagedProjectSystemCommandSet = "{568ABDF7-D522-474D-9EED-34B5E5095BA5}";
        public const long GenerateNuGetPackageProjectContextMenuCmdId = 0x2000;
        public const long GenerateNuGetPackageTopLevelBuildCmdId = 0x2001;
        public const int DebugTargetMenuDebugFrameworkMenu = 0x3000;
        public const int DebugFrameworksCmdId = 0x3050;

        public const string DefaultCapabilities = ProjectCapability.AppDesigner + "; " +
                                                  ProjectCapability.EditAndContinue + "; " +
                                                  ProjectCapability.HandlesOwnReload + "; " +
                                                  ProjectCapability.OpenProjectFile + "; " +
                                                  ProjectCapability.PreserveFormatting + "; " +
                                                  ProjectCapability.ProjectConfigurationsDeclaredDimensions + "; " +
                                                  ProjectCapability.LanguageService;

        private  IDotNetCoreProjectCompatibilityDetector _dotNetCoreCompatibilityDetector;

        public ManagedProjectSystemPackage()
        {
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IComponentModel componentModel = (IComponentModel)(await GetServiceAsync(typeof(SComponentModel)).ConfigureAwait(true));
            ICompositionService compositionService = componentModel.DefaultCompositionService;
            var debugFrameworksCmd = componentModel.DefaultExportProvider.GetExport<DebugFrameworksDynamicMenuCommand>();

            var mcs = (await GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(true)) as OleMenuCommandService;
            mcs.AddCommand(debugFrameworksCmd.Value);

            var debugFrameworksMenuTextUpdater = componentModel.DefaultExportProvider.GetExport<DebugFrameworkPropertyMenuTextUpdater>();
            mcs.AddCommand(debugFrameworksMenuTextUpdater.Value);

            // Need to use the CPS export provider to get the dotnet compatibility detector
            Lazy<IProjectServiceAccessor> projectServiceAccessor = componentModel.DefaultExportProvider.GetExport<IProjectServiceAccessor>();
            _dotNetCoreCompatibilityDetector = projectServiceAccessor.Value.GetProjectService().Services.ExportProvider.GetExport<IDotNetCoreProjectCompatibilityDetector>().Value;
            await _dotNetCoreCompatibilityDetector.InitializeAsync().ConfigureAwait(true);

#if DEBUG
            DebuggerTraceListener.RegisterTraceListener();
#endif
        }
    }
}
