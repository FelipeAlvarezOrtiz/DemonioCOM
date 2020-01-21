using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace DemonioCOM
{
    class Program
    {
        static void Main(string[] args)
        {
            var exitCode = HostFactory.Run( x=>
            {
                x.Service<DemonioCOM>( s =>
                {
                    s.ConstructUsing(daemon => new DemonioCOM());
                    s.WhenStarted(daemon => daemon.Start());
                    s.WhenStopped(daemon => daemon.Stop());
                });

                x.RunAsLocalSystem();

                x.SetServiceName("DemonioCOM");
                x.SetDisplayName("Demonio COM");
                x.SetDescription("Servicio asociado a ERP Comercial Capel para dar apoyo a los refractos y romanas." +
                                 " Servicio necesario para el correcto funcionamiento de la toma de datos desde puertos COM. " +
                                 " Desarrollado por Felipe Alvarez");
            });

            int exitCodeValue = (int)Convert.ChangeType(exitCode,exitCode.GetTypeCode());
            Environment.ExitCode = exitCodeValue;

        }
    }
}
