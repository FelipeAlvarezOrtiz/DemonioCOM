using System;
using System.Data.SQLite;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Timers;

namespace DemonioCOM
{
    class DemonioCOM
    {
        private readonly Timer _timer;
        private readonly SerialPort puertoSerial;
        private readonly static string PathDB = @"C:/ROMANA/REFRACTO/DB/Refracto.db";
        private readonly static string PathConfig = @"C:/ROMANA/REFRACTO/config.txt";
        //private static string PathMediciones = @"C:/Romana/Refracto/MEDICIONES.txt";
        private string[] valoresPuertoSerial;
        private string valorLeidoCOM = string.Empty;
        private readonly static string PathLog = @"C:/ROMANA/REFRACTO/LOG/log.txt";
        private int TicketActual = 0;

        public DemonioCOM()
        {
            _timer = new Timer(1000) { AutoReset = true };
            _timer.Elapsed += TimerElapsed;
            Obtener_ConfiguracionCOM();
            puertoSerial = new SerialPort()
            {
                PortName = valoresPuertoSerial[0],
                BaudRate = (int)Convert.ToInt32(valoresPuertoSerial[1]),
                DataBits = (int)Convert.ToInt32(valoresPuertoSerial[2]),
                Parity = Parity.None,
                Handshake = Handshake.None,
                DiscardNull = true,
            };

        }

        private void Obtener_ConfiguracionCOM()
        {
            valoresPuertoSerial = File.ReadAllText(PathConfig).Split(';');
        }
        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            //Cada vez que pasa un segundo
            if (Ejecutar_COM())
            {
                Leer_Grado();
                InsertarEnBD(valorLeidoCOM);
                TicketActual = 0;
            }
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private void EscribirEnLog(string text)
        {
            string[] nuevaLinea = new string[] { DateTime.Now.ToString() + " "+text};
            File.AppendAllLines(PathLog,nuevaLinea);
        }

        private void InsertarEnBD(string Grado)
        {
            using (var conexion = new SQLiteConnection("DataSource=" + PathDB + ";Version=3"))
            using (var command = new SQLiteCommand("UPDATE RefractoINFO_Table SET Estado_Ticket = @Estado, Grado = @Grado " +
                                                    "where Id_ticket = "+TicketActual,conexion))
            {
                try
                {
                    conexion.Open();
                    command.Parameters.Add("@Estado",System.Data.DbType.Int32).Value = 2;
                    //command.Parameters.Add("@Temperatura",System.Data.DbType.Decimal).Value = Decimal.Round(Decimal.Parse(Temperatura),2);
                    command.Parameters.Add("@Grado", System.Data.DbType.String).Value = Grado;
                        //Decimal.Round(Decimal.Parse(Grado),2);
                    command.ExecuteNonQuery();
                    EscribirEnLog("Actualización de Estado a ticket :"+TicketActual+" realizada con exito con grado: "+Grado);
                    conexion.Close();
                }
                catch (Exception e)
                {
                    EscribirEnLog("Error al actualizar la base de datos con ticket :"+TicketActual);
                    //EscribirEnLog(e.StackTrace);
                }
            }
        }

        private void Leer_Grado()
        {
            try
            {
                puertoSerial.Open();
                var lectura = puertoSerial.ReadExisting();
                var limpieza = new string(lectura.Take(5).ToArray()).Replace("M","");
                limpieza.Replace(",",".");
                valorLeidoCOM = limpieza;
                EscribirEnLog("Lectura de grado exitosa. Grado: "+valorLeidoCOM+" para ticket: "+TicketActual);
                puertoSerial.Close();
            }
            catch (Exception)
            {
                EscribirEnLog("Error al leer grado desde Puerta serial, verificar configuración");
            }
        }


        private bool Ejecutar_COM()
        {
            using (var conexion = new SQLiteConnection("DataSource ="+PathDB+";Version=3"))
            using (var command = new SQLiteCommand("Select Id_Ticket from RefractoINFO_Table where Estado_Ticket = 1",conexion))
            {
                try
                {
                    conexion.Open();
                    command.ExecuteNonQuery();
                    var count = Convert.ToInt32(command.ExecuteScalar());
                    if (count > 0)
                    {
                        TicketActual = Convert.ToInt32(command.ExecuteScalar());
                        EscribirEnLog("Ticket :"+TicketActual+" encontrado con Estado 1, realizando medición.");
                        conexion.Close();
                        return true;
                        
                    }
                    else
                    {
                        EscribirEnLog("Ningún ticket encontrado para ser medido, reintentando en 1 segundo.");
                        conexion.Close();
                        return false;
                    }
                }
                catch (Exception)
                {
                    EscribirEnLog("Error al abrir la base de datos, Reintentando en 1 segundo ...");
                    return false;
                }
            }
        }
    }
}
