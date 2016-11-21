using System.Reflection;
using System.Runtime.CompilerServices;
using Xamarin.Forms.Internals;

// Information about this assembly is defined by the following attributes.
// Change them to the values specific to your project.

#if !MXBUILD
[assembly: AssemblyTitle("Xamarin.Forms.Xaml")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCulture("")]
#endif
// The assembly version has the format "{Major}.{Minor}.{Build}.{Revision}".
// The form "{Major}.{Minor}.*" will automatically update the build and revision,
// and "{Major}.{Minor}.{Build}.*" will update just the revision.

//[assembly: AssemblyVersion ("1.0.*")]

// The following attributes are used to specify the signing key for the assembly,
// if desired. See the Mono documentation for more information about signing.
//[assembly: AssemblyDelaySign(false)]
//[assembly: AssemblyKeyFile("")]

[assembly: InternalsVisibleTo("Xamarin.Forms.Xaml.UnitTests")]
[assembly: InternalsVisibleTo("Xamarin.Forms.Build.Tasks")]
[assembly: InternalsVisibleTo("Xamarin.Forms.Xaml.Design")]
[assembly: Preserve]