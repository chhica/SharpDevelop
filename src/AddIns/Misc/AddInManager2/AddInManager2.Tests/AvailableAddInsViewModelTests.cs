﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ICSharpCode.AddInManager2.Model;
using ICSharpCode.AddInManager2.Tests.Fakes;
using ICSharpCode.AddInManager2.ViewModel;
using ICSharpCode.Core;
using NuGet;
using NUnit.Framework;

namespace ICSharpCode.AddInManager2.Tests
{
	public class AvailableAddInsViewModelTests
	{
		private const string SharpDevelopAddInTag = " sharpdevelopaddin ";
		
		FakeAddInManagerServices _services;
		
		AddIn _addIn1;
		AddIn _addIn1_new;
		AddIn _addIn2;
		AddIn _addIn2_new;
		
		public AvailableAddInsViewModelTests()
		{
		}
		
		private void CreateAddIns()
		{
			// Create AddIn objects from *.addin files available in this assembly's output directory
			FakeAddInTree _addInTree = new FakeAddInTree();

			using (StreamReader streamReader = new StreamReader(@"TestResources\AddInManager2Test.addin"))
			{
				_addIn1 = AddIn.Load(_addInTree, streamReader);
			}
			
			using (StreamReader streamReader = new StreamReader(@"TestResources\AddInManager2Test_New.addin"))
			{
				_addIn1_new = AddIn.Load(_addInTree, streamReader);
			}
			
			using (StreamReader streamReader = new StreamReader(@"TestResources\AddInManager2Test_2.addin"))
			{
				_addIn2 = AddIn.Load(_addInTree, streamReader);
			}
			
			using (StreamReader streamReader = new StreamReader(@"TestResources\AddInManager2Test_2_New.addin"))
			{
				_addIn2_new = AddIn.Load(_addInTree, streamReader);
			}
		}
		
		[SetUp]
		public void SetUp()
		{
			_services = new FakeAddInManagerServices();
			_services.FakeSDAddInManagement = new FakeSDAddInManagement();
			_services.Events = new AddInManagerEvents();
			_services.FakeSetup = new FakeAddInSetup(_services.SDAddInManagement);
			_services.FakeSettings = new FakeAddInManagerSettings();
			_services.FakeRepositories = new FakePackageRepositories();
			_services.FakeNuGet = new FakeNuGetPackageManager();
			
			// Create SynchronizationContext needed for the view model
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
		}
		
		[Test]
		public void ShowInstallableAddIns()
		{
			CreateAddIns();
			_addIn1.Enabled = true;
			
			// Packages to be shown in repository
			FakePackage fakePackage1_old = new FakePackage()
			{
				Id = _addIn1.Manifest.PrimaryIdentity,
				Version = new SemanticVersion(_addIn1.Version),
				Tags = SharpDevelopAddInTag
			};
			FakePackage fakePackage1_new = new FakePackage()
			{
				Id = _addIn1_new.Manifest.PrimaryIdentity,
				Version = new SemanticVersion(_addIn1_new.Version),
				Tags = SharpDevelopAddInTag
			};
			FakePackage fakePackage2 = new FakePackage()
			{
				Id = _addIn2.Manifest.PrimaryIdentity,
				Version = new SemanticVersion(_addIn2.Version),
				Tags = SharpDevelopAddInTag
			};
			
			// List of NuGet repositories
			List<PackageSource> registeredPackageSources = new List<PackageSource>();
			registeredPackageSources.Add(new PackageSource("", "Test Repository"));
			_services.FakeRepositories.RegisteredPackageSources = registeredPackageSources;
			
			List<IPackageRepository> registeredPackageRepositories = new List<IPackageRepository>();
			FakeCorePackageRepository remoteRepository = new FakeCorePackageRepository();
			remoteRepository.Source = registeredPackageSources[0].Source;
			remoteRepository.ReturnedPackages = (new IPackage[] { fakePackage1_old, fakePackage1_new, fakePackage2 }).AsQueryable();
			_services.FakeRepositories.RegisteredPackageRepositories = registeredPackageRepositories;
			
			// PackageRepository service should return remoteRepository instance
			_services.FakeRepositories.GetRepositoryFromSourceCallback = delegate(PackageSource packageSource)
			{
				return remoteRepository;
			};
			
			FakeCorePackageRepository localRepository = new FakeCorePackageRepository();
			_services.FakeNuGet.FakeCorePackageManager.LocalRepository = localRepository;
			localRepository.ReturnedPackages = (new IPackage[] { }).AsQueryable();
			
			var viewModel = new AvailableAddInsViewModel(_services);
			viewModel.ReadPackagesAndWaitForUpdate();
			
			Assert.That(viewModel.AddInPackages.Count, Is.EqualTo(2), "AddIn list must contain 2 items.");
			
			AddInPackageViewModelBase firstAddIn = viewModel.AddInPackages[0];
			Assert.That(firstAddIn.Id, Is.EqualTo(_addIn1_new.Manifest.PrimaryIdentity), "Primary identity of 1st AddIn");
			Assert.That(firstAddIn.Name, Is.EqualTo(_addIn1_new.Manifest.PrimaryIdentity), "Name of 1st AddIn");
			Assert.That(firstAddIn.Version, Is.EqualTo(_addIn1_new.Version), "Version of 1st AddIn");
			Assert.That(firstAddIn.IsInstalled, Is.False, "1st AddIn must not be 'installed''");
			Assert.That(firstAddIn.IsOffline, Is.False, "1st AddIn must not be 'offline'");
			Assert.That(firstAddIn.IsEnabled, Is.True, "1st AddIn must be 'enabled'");
			Assert.That(firstAddIn.IsUpdate, Is.False, "1st AddIn must not be 'update'");
			Assert.That(firstAddIn.IsAdded, Is.False, "1st AddIn must not be 'added'");
			Assert.That(firstAddIn.IsRemoved, Is.False, "1st AddIn must not be 'removed'");
			Assert.That(firstAddIn.HasNuGetConnection, Is.False, "1st AddIn must not have 'NuGet connection'");
			Assert.That(viewModel.AddInPackages[0].IsExternallyReferenced, Is.False, "1st AddIn must not be 'externally referenced'");
			Assert.That(viewModel.AddInPackages[1].IsExternallyReferenced, Is.False, "2nd AddIn must not be 'externally referenced'");
		}
		
		[Test]
		public void SearchInstallableAddIns()
		{
			CreateAddIns();
			_addIn1.Enabled = true;
			
			// Package to be shown in repository
			FakePackage fakePackage1 = new FakePackage()
			{
				Id = _addIn1.Manifest.PrimaryIdentity,
				Version = new SemanticVersion(_addIn1.Version),
				Tags = SharpDevelopAddInTag
			};
			FakePackage fakePackage2 = new FakePackage()
			{
				Id = _addIn2.Manifest.PrimaryIdentity,
				Version = new SemanticVersion(_addIn2.Version),
				Tags = SharpDevelopAddInTag
			};
			
			// List of NuGet repositories
			List<PackageSource> registeredPackageSources = new List<PackageSource>();
			registeredPackageSources.Add(new PackageSource("", "Test Repository"));
			_services.FakeRepositories.RegisteredPackageSources = registeredPackageSources;
			
			List<IPackageRepository> registeredPackageRepositories = new List<IPackageRepository>();
			FakeCorePackageRepository remoteRepository = new FakeCorePackageRepository();
			remoteRepository.Source = registeredPackageSources[0].Source;
			remoteRepository.ReturnedPackages = (new IPackage[] { fakePackage1, fakePackage2 }).AsQueryable();
			_services.FakeRepositories.RegisteredPackageRepositories = registeredPackageRepositories;
			
			// PackageRepository service should return remoteRepository instance
			_services.FakeRepositories.GetRepositoryFromSourceCallback = delegate(PackageSource packageSource)
			{
				return remoteRepository;
			};
			
			FakeCorePackageRepository localRepository = new FakeCorePackageRepository();
			_services.FakeNuGet.FakeCorePackageManager.LocalRepository = localRepository;
			localRepository.ReturnedPackages = (new IPackage[] { }).AsQueryable();
			
			var viewModel = new AvailableAddInsViewModel(_services);
			viewModel.SearchTerms = fakePackage2.Id;
			viewModel.ReadPackagesAndWaitForUpdate();
			
			Assert.That(viewModel.AddInPackages.Count, Is.EqualTo(1), "AddIn list must contain 1 item.");
			
			AddInPackageViewModelBase firstAddIn = viewModel.AddInPackages[0];
			Assert.That(firstAddIn.Id, Is.EqualTo(_addIn2.Manifest.PrimaryIdentity), "Primary identity of 1st AddIn");
			Assert.That(firstAddIn.Name, Is.EqualTo(_addIn2.Manifest.PrimaryIdentity), "Name of 1st AddIn");
			Assert.That(firstAddIn.Version, Is.EqualTo(_addIn2.Version), "Version of 1st AddIn");
		}
		
		[Test]
		public void FilteringOutNonSharpDevelopPackages()
		{
			CreateAddIns();
			_addIn1.Enabled = true;
			
			// Package to be shown in repository
			FakePackage fakePackage1 = new FakePackage()
			{
				Id = _addIn1.Manifest.PrimaryIdentity,
				Version = new SemanticVersion(_addIn1.Version)
			};
			FakePackage fakePackage2 = new FakePackage()
			{
				Id = _addIn2.Manifest.PrimaryIdentity,
				Version = new SemanticVersion(_addIn2.Version),
				Tags = SharpDevelopAddInTag
			};
			
			// List of NuGet repositories
			List<PackageSource> registeredPackageSources = new List<PackageSource>();
			registeredPackageSources.Add(new PackageSource("", "Test Repository"));
			_services.FakeRepositories.RegisteredPackageSources = registeredPackageSources;
			
			List<IPackageRepository> registeredPackageRepositories = new List<IPackageRepository>();
			FakeCorePackageRepository remoteRepository = new FakeCorePackageRepository();
			remoteRepository.Source = registeredPackageSources[0].Source;
			remoteRepository.ReturnedPackages = (new IPackage[] { fakePackage1, fakePackage2 }).AsQueryable();
			_services.FakeRepositories.RegisteredPackageRepositories = registeredPackageRepositories;
			
			// PackageRepository service should return remoteRepository instance
			_services.FakeRepositories.GetRepositoryFromSourceCallback = delegate(PackageSource packageSource)
			{
				return remoteRepository;
			};
			
			FakeCorePackageRepository localRepository = new FakeCorePackageRepository();
			_services.FakeNuGet.FakeCorePackageManager.LocalRepository = localRepository;
			localRepository.ReturnedPackages = (new IPackage[] { }).AsQueryable();
			
			var viewModel = new AvailableAddInsViewModel(_services);
			viewModel.ReadPackagesAndWaitForUpdate();
			
			Assert.That(viewModel.AddInPackages.Count, Is.EqualTo(1), "AddIn list must contain 1 item.");
			
			AddInPackageViewModelBase firstAddIn = viewModel.AddInPackages[0];
			Assert.That(firstAddIn.Id, Is.EqualTo(_addIn2.Manifest.PrimaryIdentity), "Primary identity of 1st AddIn");
			Assert.That(firstAddIn.Name, Is.EqualTo(_addIn2.Manifest.PrimaryIdentity), "Name of 1st AddIn");
			Assert.That(firstAddIn.Version, Is.EqualTo(_addIn2.Version), "Version of 1st AddIn");
		}
		
		[Test]
		public void ShowAlreadyInstalledAddIns()
		{
			CreateAddIns();
			_addIn1.Enabled = true;
			
			// Package to be shown in repository
			FakePackage fakePackage1_old = new FakePackage()
			{
				Id = _addIn1.Manifest.PrimaryIdentity,
				Version = new SemanticVersion(_addIn1.Version),
				Tags = SharpDevelopAddInTag
			};
			FakePackage fakePackage1_new = new FakePackage()
			{
				Id = _addIn1_new.Manifest.PrimaryIdentity,
				Version = new SemanticVersion(_addIn1_new.Version),
				Tags = SharpDevelopAddInTag
			};
			FakePackage fakePackage2 = new FakePackage()
			{
				Id = _addIn2.Manifest.PrimaryIdentity,
				Version = new SemanticVersion(_addIn2.Version),
				Tags = SharpDevelopAddInTag
			};
			
			_addIn1.Properties.Set(ManagedAddIn.NuGetPackageIDManifestAttribute, fakePackage1_old.Id);
			_addIn1.Properties.Set(ManagedAddIn.NuGetPackageVersionManifestAttribute, fakePackage1_old.Version.ToString());
			_addIn2.Properties.Set(ManagedAddIn.NuGetPackageIDManifestAttribute, fakePackage2.Id);
			
			// List of NuGet repositories
			List<PackageSource> registeredPackageSources = new List<PackageSource>();
			registeredPackageSources.Add(new PackageSource("", "Test Repository"));
			_services.FakeRepositories.RegisteredPackageSources = registeredPackageSources;
			
			List<IPackageRepository> registeredPackageRepositories = new List<IPackageRepository>();
			FakeCorePackageRepository remoteRepository = new FakeCorePackageRepository();
			remoteRepository.Source = registeredPackageSources[0].Source;
			remoteRepository.ReturnedPackages = (new IPackage[] { fakePackage1_new, fakePackage2 }).AsQueryable();
			_services.FakeRepositories.RegisteredPackageRepositories = registeredPackageRepositories;
			
			// PackageRepository service should return remoteRepository instance
			_services.FakeRepositories.GetRepositoryFromSourceCallback = delegate(PackageSource packageSource)
			{
				return remoteRepository;
			};
			
			FakeCorePackageRepository localRepository = new FakeCorePackageRepository();
			_services.FakeNuGet.FakeCorePackageManager.LocalRepository = localRepository;
			localRepository.ReturnedPackages = (new IPackage[] { fakePackage1_old, fakePackage2 }).AsQueryable();
			
			// Simulate list of AddIns
			_services.FakeSDAddInManagement.RegisteredAddIns.Add(_addIn1);
			_services.FakeSDAddInManagement.RegisteredAddIns.Add(_addIn2);
			
			// Simulation of resolving AddIns <-> NuGet packages
			_services.FakeSetup.GetAddInForNuGetPackageCallback = delegate(IPackage package, bool withAddInsMarkedForInstallation)
			{
				if (package.Id == _addIn1.Properties[ManagedAddIn.NuGetPackageIDManifestAttribute])
				{
					return _addIn1;
				}
				else if (package.Id == _addIn2.Properties[ManagedAddIn.NuGetPackageIDManifestAttribute])
				{
					return _addIn2;
				}
				
				return null;
			};
			
			var viewModel = new AvailableAddInsViewModel(_services);
			viewModel.ReadPackagesAndWaitForUpdate();
			
			Assert.That(viewModel.AddInPackages.Count, Is.EqualTo(2), "AddIn list must contain 2 items.");
			
			AddInPackageViewModelBase firstAddIn = viewModel.AddInPackages[0];
			Assert.That(firstAddIn.Id, Is.EqualTo(_addIn1_new.Manifest.PrimaryIdentity), "Primary identity of 1st AddIn");
			Assert.That(firstAddIn.Name, Is.EqualTo(_addIn1_new.Manifest.PrimaryIdentity), "Name of 1st AddIn");
			Assert.That(firstAddIn.Version, Is.EqualTo(_addIn1_new.Version), "Version of 1st AddIn");
			Assert.That(firstAddIn.IsInstalled, Is.True, "1st AddIn must be 'installed''");
			Assert.That(firstAddIn.IsOffline, Is.False, "1st AddIn must not be 'offline'");
			Assert.That(firstAddIn.IsEnabled, Is.True, "1st AddIn must be 'enabled'");
			Assert.That(firstAddIn.IsUpdate, Is.True, "1st AddIn must be 'update'");
			Assert.That(firstAddIn.IsAdded, Is.False, "1st AddIn must not be 'added'");
			Assert.That(firstAddIn.IsRemoved, Is.False, "1st AddIn must not be 'removed'");
			Assert.That(firstAddIn.HasNuGetConnection, Is.False, "1st AddIn must not have 'NuGet connection'");
			Assert.That(viewModel.AddInPackages[0].IsExternallyReferenced, Is.False, "1st AddIn must not be 'externally referenced'");
			Assert.That(viewModel.AddInPackages[1].IsExternallyReferenced, Is.False, "2nd AddIn must not be 'externally referenced'");
			
			AddInPackageViewModelBase secondAddIn = viewModel.AddInPackages[1];
			Assert.That(secondAddIn.Id, Is.EqualTo(_addIn2.Manifest.PrimaryIdentity), "Primary identity of 2nd AddIn");
			Assert.That(secondAddIn.Name, Is.EqualTo(_addIn2.Manifest.PrimaryIdentity), "Name of 2nd AddIn");
			Assert.That(secondAddIn.Version, Is.EqualTo(_addIn2.Version), "Version of 2nd AddIn");
			Assert.That(secondAddIn.IsInstalled, Is.True, "2nd AddIn must be 'installed''");
			Assert.That(secondAddIn.IsOffline, Is.False, "2nd AddIn must not be 'offline'");
			Assert.That(secondAddIn.IsEnabled, Is.True, "2nd AddIn must be 'enabled'");
			Assert.That(secondAddIn.IsUpdate, Is.False, "2nd AddIn mustnot  be 'update'");
			Assert.That(secondAddIn.IsAdded, Is.False, "2nd AddIn must not be 'added'");
			Assert.That(secondAddIn.IsRemoved, Is.False, "2nd AddIn must not be 'removed'");
			Assert.That(secondAddIn.HasNuGetConnection, Is.False, "2nd AddIn must not have 'NuGet connection'");
		}
	}
}
