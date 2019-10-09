using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Reproductor = Android.Media;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Content;
using Android.Preferences;
using Android.Runtime;

namespace loteria
{
    [Activity(Label = "mesa_de_juego", Theme = "@android:style/Theme.Holo.Light.NoActionBar.Fullscreen")]
    public class mesa_de_juego : Activity, Reproductor.MediaPlayer.IOnErrorListener
    {
        //alert de trampa
        View custom_alert_trampa;
        AlertDialog.Builder builder_trampa;
        AlertDialog alert_trampa;
        EditText jugador_tramposo;

        //variable para reproducir la musica del juego
        bool resproducir_primera_vez = true;

        //variable para repartir el sonido de las cartas en dos instancias de mediaplayer
        bool unoCadaUno = true;

        //variables para reproducir las cartas
        Reproductor.MediaPlayer player_carta1;
        Reproductor.MediaPlayer player_carta2;

        //varible para reaunudar la partida
        bool partida_pausada = false;

        //reprouctores de musica y efectos de sonido
        Reproductor.MediaPlayer player_musica_fondo;
        Reproductor.MediaPlayer player_musica_busqueda;
        Reproductor.MediaPlayer player_musica_juego;

        //reproducir sonido de trampa
        Reproductor.MediaPlayer reproductor_trampa;
        Reproductor.MediaPlayer reproductor_trampa_musica;


        //contador para saber si se seleccionaon todas las cartas antes de ganar
        int contador_cartas = 0;

        //variable para guardar el nombre del jugador y la ip del servidor
        string jugador = "";
        string IP_server = "";

        //lista de cartas pasadas
        List<int> cartasPasadas = new List<int>();

        //lista de cartas que forman la carta del cliente
        List<int> cartaCliente = new List<int>();

        //lista de cartas que forman la carta del server
        List<int> cartaServer = new List<int>();

        #region cosas para el cliente;

        //objetos que se utilizan para conectar con el servidor
        Socket listen_cliente = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint connect_cliente = null;

        //hilo para recibir las cartas del server
        Thread hilo_recibir_carta;

        #endregion;

        #region cosas para el server;
        //TextView de los jugadores global para poder manipularlo en el hilo que ecibe a los jugadores que se conectan 
        TextView txt_jugadores = null;
        //manejadores de sockets para conectar a los juadores
        Socket listen_server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        Socket conexion;
        IPEndPoint connect_server = new IPEndPoint(obtenerIP(), 8000);

        //lista para guardar a los jugadores que se van uniendo a la partida
        List<jugador_model> Clientes = new List<jugador_model>();

        //timer para enviar cartas a los clientes
        System.Timers.Timer timerCartas = new System.Timers.Timer();

        //hilo para esperar a los jugadores que se uniran a la partida
        Thread hilo_espear_jugadores;

        //hilo para esperar que alguien gane
        Thread hilo_espear_ganador;

        //referencia a objetos de la interfaz
        ImageView carta_actual;
        ImageView carta_anterior;
        LinearLayout btn_buenas;

        #endregion;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Window.AddFlags(WindowManagerFlags.Fullscreen);
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);
            Window.AddFlags(WindowManagerFlags.TranslucentNavigation);

            //cargar alert de trampa
            custom_alert_trampa = LayoutInflater.Inflate(Resource.Layout.alerta_trampa, null);
            builder_trampa = new AlertDialog.Builder(this);
            alert_trampa = builder_trampa.Create();
            jugador_tramposo = custom_alert_trampa.FindViewById<EditText>(Resource.Id.txt_jugador_trampa);

            //colocar la musica a cada respectivo objeto
            player_musica_fondo = Reproductor.MediaPlayer.Create(this, Resource.Raw.BackgroundMusic);
            player_musica_busqueda = Reproductor.MediaPlayer.Create(this, Resource.Raw.SearchSound);
            player_musica_juego = Reproductor.MediaPlayer.Create(this, Resource.Raw.GameMusic);

            //agregar los loops
            player_musica_juego.Looping = true;
            player_musica_juego.SetVolume(0.2f, 0.2f);
            player_musica_fondo.Looping = true;

            //reproducir muscia de fondo
            player_musica_fondo.Start();
            player_musica_fondo.SetVolume(0.5f, 0.5f);

            SetContentView(Resource.Layout.mesa_de_juego);
            // Create your application here

            //obtener el valor que envio el activity del menu
            jugador = Intent.GetStringExtra("jugador") ?? "no se obtuvo";
            var cliente = Intent.GetStringExtra("cliente") ?? "no";

            //obtener todas las partes del juego 
            carta_actual = FindViewById<ImageView>(Resource.Id.img_carta_actual);
            carta_anterior = FindViewById<ImageView>(Resource.Id.img_carta_anterior);
            btn_buenas = FindViewById<LinearLayout>(Resource.Id.btn_buenas);

            //deshabilitar el boton de buenas hasta que inicie la partida 
            btn_buenas.Enabled = false;

            var carta1 = FindViewById<ImageView>(Resource.Id.carta1);
            var carta2 = FindViewById<ImageView>(Resource.Id.carta2);
            var carta3 = FindViewById<ImageView>(Resource.Id.carta3);
            var carta4 = FindViewById<ImageView>(Resource.Id.carta4);
            var carta5 = FindViewById<ImageView>(Resource.Id.carta5);
            var carta6 = FindViewById<ImageView>(Resource.Id.carta6);
            var carta7 = FindViewById<ImageView>(Resource.Id.carta7);
            var carta8 = FindViewById<ImageView>(Resource.Id.carta8);
            var carta9 = FindViewById<ImageView>(Resource.Id.carta9);
            var carta10 = FindViewById<ImageView>(Resource.Id.carta10);
            var carta11 = FindViewById<ImageView>(Resource.Id.carta11);
            var carta12 = FindViewById<ImageView>(Resource.Id.carta12);
            var carta13 = FindViewById<ImageView>(Resource.Id.carta13);
            var carta14 = FindViewById<ImageView>(Resource.Id.carta14);
            var carta15 = FindViewById<ImageView>(Resource.Id.carta15);
            var carta16 = FindViewById<ImageView>(Resource.Id.carta16);

            //evaluar cartas al presionarlas 
            carta1.Click += delegate { evaluar_carta(carta1); };
            carta2.Click += delegate { evaluar_carta(carta2); };
            carta3.Click += delegate { evaluar_carta(carta3); };
            carta4.Click += delegate { evaluar_carta(carta4); };
            carta5.Click += delegate { evaluar_carta(carta5); };
            carta6.Click += delegate { evaluar_carta(carta6); };
            carta7.Click += delegate { evaluar_carta(carta7); };
            carta8.Click += delegate { evaluar_carta(carta8); };
            carta9.Click += delegate { evaluar_carta(carta9); };
            carta10.Click += delegate { evaluar_carta(carta10); };
            carta11.Click += delegate { evaluar_carta(carta11); };
            carta12.Click += delegate { evaluar_carta(carta12); };
            carta13.Click += delegate { evaluar_carta(carta13); };
            carta14.Click += delegate { evaluar_carta(carta14); };
            carta15.Click += delegate { evaluar_carta(carta15); };
            carta16.Click += delegate { evaluar_carta(carta16); };

            //programa el boton de buenas
            btn_buenas.Click += async delegate {

                if (contador_cartas==16)
                {
                    if (cliente == "si")
                        reclamar_buenas(false);
                    else
                    {
                        //pausar la partida
                        timerCartas.Stop();

                        //revisar carta
                        var contCartas = 0;
                        for (int i = 0; i < cartaServer.Count; i++)
                        {
                            if (cartasPasadas.Contains(cartaServer[i]))
                            {
                                contCartas++;
                            }
                        }


                        if (contCartas == 16)
                        {
                            //detener la partida y decirles a todos quien gano
                            byte[] mensaje = new byte[100];

                            mensaje = Encoding.Default.GetBytes("perdiste$" + jugador);
                            Console.WriteLine(mensaje);

                            foreach (var item in Clientes)
                            {
                                item.socket_jugador.Send(mensaje);
                            }

                            timerCartas.Stop();

                            //detener todos los hilos
                            hilo_espear_ganador.Abort();

                            //mostrar alert de ganador
                            View custom_alert = LayoutInflater.Inflate(Resource.Layout.alerta_ganador, null);
                            AlertDialog.Builder builder = new AlertDialog.Builder(this);
                            AlertDialog alert = builder.Create();
                            alert.SetView(custom_alert);
                            alert.SetCancelable(false);

                            //reproducir sonido de ganador
                            Reproductor.MediaPlayer reproductor_ganador = Reproductor.MediaPlayer.Create(this, Resource.Raw.ganaste);
                            Reproductor.MediaPlayer reproductor_ganador_musica = Reproductor.MediaPlayer.Create(this, Resource.Raw.win);

                            reproductor_ganador.Start();
                            reproductor_ganador_musica.Start();

                            custom_alert.FindViewById<TextView>(Resource.Id.lbl_nombre_usuario).Text = "Ganaste " + jugador;

                            custom_alert.FindViewById<Button>(Resource.Id.btn_menu).Click += delegate
                            {
                                player_musica_fondo.Stop();
                                player_musica_busqueda.Stop();
                                player_musica_juego.Stop();
                                //guardar bandera para reproducir la musica
                                ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
                                prefs = PreferenceManager.GetDefaultSharedPreferences(this);
                                ISharedPreferencesEditor editor = prefs.Edit();
                                editor.PutBoolean("pref_back_from_game", true);
                                editor.Apply();

                                //detener la musica
                                reproductor_ganador.Stop();
                                reproductor_ganador.Release();
                                reproductor_ganador_musica.Stop();
                                reproductor_ganador_musica.Release();
                                this.Finish();
                            };
                            alert.Show();

                        }
                        else
                        {
                            byte[] mensaje = new byte[100];
                            mensaje = Encoding.Default.GetBytes("trampa$" + jugador);
                            Console.WriteLine(mensaje);

                            foreach (var item in Clientes)
                            {
                                item.socket_jugador.Send(mensaje);
                            }

                            alert_trampa.SetView(custom_alert_trampa);
                            alert_trampa.SetCancelable(false);
                            jugador_tramposo.Text = jugador;
                            alert_trampa.Show();

                            //reproducir sonido de trampa
                            reproductor_trampa = Reproductor.MediaPlayer.Create(this, Resource.Raw.trampa);
                            reproductor_trampa_musica = Reproductor.MediaPlayer.Create(this, Resource.Raw.cheat);

                            reproductor_trampa.Start();
                            reproductor_trampa_musica.Start();

                            await Task.Delay(4000);
                            timerCartas.Start();

                            reproductor_trampa.Stop();
                            reproductor_trampa_musica.Release();

                            alert_trampa.Dismiss();
                        }
                    }
                }

            };

            //cargar el alert para conectarse con el servidor si se es cliente
            //si es server abre un alert para esperar a los clientes
            if (cliente == "si")
            {
                View custom_alert = LayoutInflater.Inflate(Resource.Layout.alerta_ip_cliente, null);
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                AlertDialog alert = builder.Create();
                alert.SetView(custom_alert);
                alert.SetCancelable(false);
                custom_alert.FindViewById<Button>(Resource.Id.btn_conectar).Click += delegate
                {
                    //conectarse al servidor 
                    conectar(custom_alert.FindViewById<EditText>(Resource.Id.txt_IP_Server).Text, alert);
                    //generar carta aleatoria
                    cartaCliente = generar_carta();

                    carta1.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaCliente[0], null, null));
                    carta2.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaCliente[1], null, null));
                    carta3.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaCliente[2], null, null));
                    carta4.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaCliente[3], null, null));
                    carta5.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaCliente[4], null, null));
                    carta6.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaCliente[5], null, null));
                    carta7.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaCliente[6], null, null));
                    carta8.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaCliente[7], null, null));
                    carta9.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaCliente[8], null, null));
                    carta10.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaCliente[9], null, null));
                    carta11.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaCliente[10], null, null));
                    carta12.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaCliente[11], null, null));
                    carta13.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaCliente[12], null, null));
                    carta14.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaCliente[13], null, null));
                    carta15.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaCliente[14], null, null));
                    carta16.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaCliente[15], null, null));

                };
                custom_alert.FindViewById<Button>(Resource.Id.btn_cancelar).Click += delegate
                {
                    player_musica_fondo.Stop();
                    player_musica_busqueda.Stop();
                    player_musica_juego.Stop();
                    //guardar bandera para reproducir la musica
                    ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
                    prefs = PreferenceManager.GetDefaultSharedPreferences(this);
                    ISharedPreferencesEditor editor = prefs.Edit();
                    editor.PutBoolean("pref_back_from_game", true);
                    editor.Apply();
                    this.Finish();
                };
                alert.Show();

                //programar el boton de rendirse para el cliente
                FindViewById<LinearLayout>(Resource.Id.btn_rendirse).Click += delegate
                {
                    reclamar_buenas(true);
                };
            }
            else
            {
                //reproducir sonido de espera
                player_musica_busqueda.Start();
                player_musica_busqueda.Looping = true;

                View custom_alert = LayoutInflater.Inflate(Resource.Layout.alert_crear_partida, null);
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                AlertDialog alert = builder.Create();
                alert.SetView(custom_alert);
                alert.SetCancelable(false);

                txt_jugadores = custom_alert.FindViewById<TextView>(Resource.Id.txt_jugadores);
                var btn_jugar = custom_alert.FindViewById<Button>(Resource.Id.btn_jugar);

                //obtener la ip del servidor para colocar en el textview
                var txt_IP = custom_alert.FindViewById<TextView>(Resource.Id.txt_IP);
                txt_IP.Text += " " + obtenerIP();

                //colocar el nombre del jugador que creo la partida en la lista de jugadores
                txt_jugadores.Text += jugador + "\r\n";

                //iniciar el servidor
                listen_server.Bind(connect_server);
                listen_server.Listen(20);

                hilo_espear_jugadores = new Thread(espear_jugadores);
                hilo_espear_jugadores.IsBackground = true;
                hilo_espear_jugadores.Start();

                custom_alert.FindViewById<Button>(Resource.Id.btn_jugar).Click += delegate
                {
                    //saber si hay clientes conectados para poder empear la partida
                    if (Clientes.Count > 0)
                    {
                        //detener la musica de fondo e iniciar la musica del juego
                        player_musica_fondo.Stop();
                        player_musica_busqueda.Stop();

                        //detener el hilo que espera jugadores
                        hilo_espear_jugadores.Abort();

                        alert.Cancel();

                        //iniciar el hilo que espera los del los jugadores por si alguien gana 
                        hilo_espear_ganador = new Thread(espear_ganador);
                        hilo_espear_ganador.IsBackground = true;
                        hilo_espear_ganador.Start();

                        //generar carta aleatoria
                        cartaServer = generar_carta();

                        carta1.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaServer[0], null, null));
                        carta2.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaServer[1], null, null));
                        carta3.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaServer[2], null, null));
                        carta4.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaServer[3], null, null));
                        carta5.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaServer[4], null, null));
                        carta6.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaServer[5], null, null));
                        carta7.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaServer[6], null, null));
                        carta8.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaServer[7], null, null));
                        carta9.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaServer[8], null, null));
                        carta10.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaServer[9], null, null));
                        carta11.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaServer[10], null, null));
                        carta12.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaServer[11], null, null));
                        carta13.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaServer[12], null, null));
                        carta14.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaServer[13], null, null));
                        carta15.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaServer[14], null, null));
                        carta16.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + cartaServer[15], null, null));

                        //generar la lista de cartas aleatorias
                        var cartasBarajadas = barajar();

                        var contadorCartas = 0;

                        //reproducir corre y se va
                        Reproductor.MediaPlayer reproductor_corre = Reproductor.MediaPlayer.Create(this, Resource.Raw.corre_y_se_va);
                        reproductor_corre.Start();
                        reproductor_corre.Completion += delegate
                        {
                            //empezar el timer para empezar las cartas
                            timerCartas.Interval = 3000;
                            timerCartas.Elapsed += (s, e) =>
                            {
                                if (contadorCartas > cartasBarajadas.Count)
                                    timerCartas.Stop();
                                else
                                {
                                    enviar_carta(cartasBarajadas[contadorCartas]);
                                    cartasPasadas.Add(cartasBarajadas[contadorCartas]);
                                    contadorCartas++;
                                }
                            };
                            timerCartas.Enabled = true;
                        };
                    }
                    else
                    {
                        Toast.MakeText(this, "no se puede iniciar una partida solo", ToastLength.Short).Show();
                    }

                };
                custom_alert.FindViewById<Button>(Resource.Id.btn_cancelar).Click += delegate
                {
                    player_musica_fondo.Stop();
                    player_musica_busqueda.Stop();
                    player_musica_juego.Stop();
                    //guardar bandera para reproducir la musica
                    ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
                    prefs = PreferenceManager.GetDefaultSharedPreferences(this);
                    ISharedPreferencesEditor editor = prefs.Edit();
                    editor.PutBoolean("pref_back_from_game", true);
                    editor.Apply();
                    this.Finish();
                };

                alert.Show();
            }
        }

        //genera la tabla de cartas para cada jugador
        List<int> generar_carta()
        {
            List<int> numeros = new List<int>();
            var stop = false;
            while (!stop)
            {
                //generar numero aleatorio
                var aleatorioNum = new Random().Next(-20, 100);
                if (aleatorioNum >= 1 && aleatorioNum <= 54)
                {
                    if (!numeros.Contains(aleatorioNum))
                    {
                        numeros.Add(aleatorioNum);
                        if (numeros.Count == 16)
                        {
                            stop = true;
                        }
                    }
                }
            }
            return numeros;
        }

        List<int> barajar()
        {
            List<int> numeros = new List<int>();
            var stop = false;
            while (!stop)
            {
                //generar numero aleatorio
                var aleatorioNum = new Random().Next(-20, 100);
                if (aleatorioNum >= 1 && aleatorioNum <= 54)
                {
                    if (!numeros.Contains(aleatorioNum))
                    {
                        numeros.Add(aleatorioNum);
                        if (numeros.Count == 54)
                        {
                            stop = true;
                        }
                    }
                }
            }
            return numeros;
        }

        void reclamar_buenas(bool rendicion)
        {
            byte[] enviar_info = new byte[100];

            if (rendicion)
            {
                enviar_info = Encoding.Default.GetBytes("me_rindo$" + obtenerIP());
            }
            else
            {
                string mensajeAEnviar = obtenerIP() + "$";
                foreach (var item in cartaCliente)
                {
                    mensajeAEnviar += item + "$";
                }
                mensajeAEnviar = mensajeAEnviar.Substring(0, mensajeAEnviar.Length - 1);
                enviar_info = Encoding.Default.GetBytes(mensajeAEnviar);
            }

            listen_cliente.Send(enviar_info);
        }

        void espear_ganador()
        {
            while (true)
            {
                //obtener el mensaje del servidor
                byte[] recibir_info = new byte[100];
                string data = "";
                int array_size = conexion.Receive(recibir_info, 0, recibir_info.Length, 0);
                Array.Resize(ref recibir_info, array_size);
                data = Encoding.Default.GetString(recibir_info);

                Console.WriteLine("mensaje de espear_ganador" + data);

                RunOnUiThread(async () =>
                {
                    if (data != "")
                    {
                        //pausar la partida
                        timerCartas.Stop();

                        //revisar carta
                        var contCartas = 0;
                        var listaDeCartas = data.Split("$");

                        //saber si el jugador reclamo buenas o se esta rindiendo 
                        if (listaDeCartas[0] == "me_rindo")
                        {

                            var cliente_existente = Clientes.Find(s => s.ip_jugador == listaDeCartas[1]);

                            Clientes.Remove(cliente_existente);

                            if (Clientes.Count <= 0)
                            {
                                //detener todos los hilos
                                hilo_espear_ganador.Abort();

                                //enviar mensaje al server para cancelar la partida
                                Toast.MakeText(this, "no hay clientes", ToastLength.Short).Show();
                            }
                        }
                        else
                        {

                            for (int i = 0; i < listaDeCartas.Length; i++)
                            {
                                if (i > 0)
                                {
                                    if (cartasPasadas.Contains(int.Parse(listaDeCartas[i])))
                                    {
                                        contCartas++;
                                    }
                                }
                            }

                            //obtener el nombre del jugador conforme su IP
                            var cliente = Clientes.Find(s => s.ip_jugador == listaDeCartas[0]);

                            if (contCartas == 16)
                            {
                                //detener la partida y decirles a todos quien gano
                                byte[] mensaje = new byte[100];

                                mensaje = Encoding.Default.GetBytes("ganador$" + cliente.nombre_jugador);
                                Console.WriteLine(mensaje);

                                foreach (var item in Clientes)
                                {
                                    item.socket_jugador.Send(mensaje);
                                }

                                timerCartas.Stop();

                                //detener todos los hilos
                                hilo_espear_ganador.Abort();

                                //enviar mensaje de pededor al server
                                View custom_alert = LayoutInflater.Inflate(Resource.Layout.alerta_perdedor, null);
                                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                                AlertDialog alert = builder.Create();
                                alert.SetView(custom_alert);
                                alert.SetCancelable(false);

                                //reproducir sonido de ganador
                                Reproductor.MediaPlayer reproductor_perdedor = Reproductor.MediaPlayer.Create(this, Resource.Raw.perdiste);
                                Reproductor.MediaPlayer reproductor_perdedor_musica = Reproductor.MediaPlayer.Create(this, Resource.Raw.fail);

                                reproductor_perdedor.Start();
                                reproductor_perdedor_musica.Start();

                                custom_alert.FindViewById<TextView>(Resource.Id.lbl_nombre_usuario).Text = "Perdiste " + jugador;
                                custom_alert.FindViewById<TextView>(Resource.Id.lbl_ganador).Text = "Gano: " + cliente.nombre_jugador;

                                custom_alert.FindViewById<Button>(Resource.Id.btn_menu).Click += delegate
                                {
                                    player_musica_fondo.Stop();
                                    player_musica_busqueda.Stop();
                                    player_musica_juego.Stop();
                                    //guardar bandera para reproducir la musica
                                    ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
                                    prefs = PreferenceManager.GetDefaultSharedPreferences(this);
                                    ISharedPreferencesEditor editor = prefs.Edit();
                                    editor.PutBoolean("pref_back_from_game", true);
                                    editor.Apply();

                                    //detener la musica
                                    reproductor_perdedor.Stop();
                                    reproductor_perdedor.Release();
                                    reproductor_perdedor_musica.Stop();
                                    reproductor_perdedor_musica.Release();

                                    this.Finish();
                                };
                                alert.Show();

                            }
                            else
                            {
                                byte[] mensaje = new byte[100];
                                mensaje = Encoding.Default.GetBytes("trampa$" + cliente.nombre_jugador);
                                Console.WriteLine(mensaje);

                                foreach (var item in Clientes)
                                {
                                    item.socket_jugador.Send(mensaje);
                                }

                                custom_alert_trampa = LayoutInflater.Inflate(Resource.Layout.alerta_trampa, null);
                                builder_trampa = new AlertDialog.Builder(this);
                                alert_trampa = builder_trampa.Create();
                                alert_trampa.SetView(custom_alert_trampa);
                                alert_trampa.SetCancelable(false);
                                custom_alert_trampa.FindViewById<EditText>(Resource.Id.txt_jugador_trampa).Text = cliente.nombre_jugador;

                                //reproducir sonido de trampa
                                reproductor_trampa = Reproductor.MediaPlayer.Create(this, Resource.Raw.trampa);
                                reproductor_trampa_musica = Reproductor.MediaPlayer.Create(this, Resource.Raw.cheat);

                                reproductor_trampa.Start();
                                reproductor_trampa_musica.Start();

                                alert_trampa.Show();

                                await Task.Delay(4000);
                                timerCartas.Start();

                                //detener la musica
                                reproductor_trampa.Stop();
                                reproductor_trampa.Release();
                                reproductor_trampa_musica.Stop();
                                reproductor_trampa_musica.Release();

                                alert_trampa.Dismiss();
                            }
                        }

                    }
                });
            }
        }

        void espear_jugadores()
        {
            while (true)
            {
                Console.WriteLine("Work");
                //sabe si la conexion fue aceptada
                conexion = listen_server.Accept();
                Console.WriteLine("Conexion Aceptada");

                //obtener el mensaje del jugador que se acaba de unir
                byte[] recibir_info = new byte[100];
                string data = "";
                int array_size = conexion.Receive(recibir_info, 0, recibir_info.Length, 0);
                Array.Resize(ref recibir_info, array_size);
                data = Encoding.Default.GetString(recibir_info);

                Console.WriteLine("mensaje de espear_jugadores" + data);

                //guardar el objeto del jugador en la lista para enviar las cartas cuando el juego comience (solo aplica cuando el juego no ha comenzado)

                var cliente_existente = Clientes.Find(s => s.ip_jugador == data.Split('$')[1]);

                if (cliente_existente == null)
                {
                    Clientes.Add(new jugador_model
                    {
                        nombre_jugador = data.Split('$')[0],
                        ip_jugador = data.Split('$')[1],
                        socket_jugador = conexion
                    });
                }
                else
                {
                    //cambia el nombre del jugador que se unio con la misma IP
                    Clientes.Remove(cliente_existente);
                    Clientes.Add(new jugador_model
                    {
                        nombre_jugador = data.Split('$')[0],
                        ip_jugador = data.Split('$')[1],
                        socket_jugador = conexion
                    });
                }

                RunOnUiThread(() =>
                {
                    //colocar el nombre del jugador que se unio en el hilo principal de la aplicacion
                    txt_jugadores.Text = jugador + "\r\n";
                    foreach (var item in Clientes)
                    {
                        txt_jugadores.Text += item.nombre_jugador + "\r\n";
                    }
                });

            }
        }

        public void evaluar_carta(ImageView carta)
        {
            if (carta.Tag.ToString() == "0")
            {
                carta.Tag = 1;
                carta.Alpha = 0.3f;
                contador_cartas++;
            }
            else
            {
                carta.Tag = 0;
                carta.Alpha = 1f;
                contador_cartas--;
            }
        }

        public bool enviar_carta(int carta) 
        {
            byte[] mensaje = new byte[100];
            string data = carta.ToString();
            mensaje = Encoding.Default.GetBytes(data);

            RunOnUiThread(() =>
            {
                if (resproducir_primera_vez)
                {
                    //detener la musica de fondo e iniciar la musica del juego
                    player_musica_fondo.Stop();
                    player_musica_juego.Start();
                    resproducir_primera_vez = false;
                }

                //habilitar el boton de buenas
                btn_buenas.Enabled = true;

                //reproducir sonido de la carta
                if (unoCadaUno)
                {
                    player_carta1 = Reproductor.MediaPlayer.Create(this, Resources.GetIdentifier(this.PackageName + ":raw/carta" + data, null, null));
                    player_carta1.Start();
                    unoCadaUno = false;
                    try
                    {
                        player_carta2.Release();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    player_carta1.SetOnErrorListener(this);
                }
                else
                {
                    player_carta2 = Reproductor.MediaPlayer.Create(this, Resources.GetIdentifier(this.PackageName + ":raw/carta" + data, null, null));
                    player_carta2.Start();
                    unoCadaUno = true;
                    try
                    {
                        player_carta1.Release();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    player_carta2.SetOnErrorListener(this);

                }

                //imprimir la carta que envio el server y poner la pasada en la anterior XD

                carta_anterior.SetImageDrawable(carta_actual.Drawable);

                carta_actual.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + data, null, null));
            });

            foreach (var item in Clientes)
            {
                item.socket_jugador.Send(mensaje);
            }

            return true;
        }

        public void recibir_carta()
        {
            while (true)
            {
                //obtener el mensaje del servidor
                byte[] recibir_info = new byte[100];
                string data = "";
                int array_size = listen_cliente.Receive(recibir_info, 0, recibir_info.Length, 0);
                Array.Resize(ref recibir_info, array_size);
                data = Encoding.Default.GetString(recibir_info);

                RunOnUiThread(async () =>
                {
                    if (resproducir_primera_vez)
                    {
                        //detener la musica de fondo e iniciar la musica del juego
                        player_musica_fondo.Stop();
                        player_musica_juego.Start();
                        resproducir_primera_vez = false;
                    }

                    //habilitar el boton de buenas
                    btn_buenas.Enabled = true;

                    Console.WriteLine("@@@@@@@resivio " + data);

                    try
                    {
                        int.Parse(data);

                        //quitar el alert de trampa en caso de que este en primer plano
                        if (partida_pausada)
                        {
                            //detener la musica
                            reproductor_trampa.Stop();
                            reproductor_trampa.Release();
                            reproductor_trampa_musica.Stop();
                            reproductor_trampa_musica.Release();

                            alert_trampa.Dismiss();
                            partida_pausada = false;
                        }

                        //reproducir sonido de la carta
                        if (unoCadaUno)
                        {
                            player_carta1 = Reproductor.MediaPlayer.Create(this, Resources.GetIdentifier(this.PackageName + ":raw/carta" + data, null, null));
                            player_carta1.Start();
                            unoCadaUno = false;
                            try
                            {
                                player_carta2.Release();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }

                            player_carta1.SetOnErrorListener(this);
                        }
                        else
                        {
                            player_carta2 = Reproductor.MediaPlayer.Create(this, Resources.GetIdentifier(this.PackageName + ":raw/carta" + data, null, null));
                            player_carta2.Start();
                            unoCadaUno = true;
                            try
                            {
                                player_carta1.Release();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }

                            player_carta2.SetOnErrorListener(this);

                        }


                        //imprimir la carta que envio el server y poner la pasada en la anterior XD
                        carta_anterior.SetImageDrawable(carta_actual.Drawable);

                        carta_actual.SetImageResource(Resources.GetIdentifier(this.PackageName + ":drawable/carta" + data, null, null));

                    }
                    catch
                    {
                        switch (data)
                        {
                            case "server_error":
                                Toast.MakeText(this, "el server se cago", ToastLength.Short).Show();
                                break;
                            default:
                                var info = data.Split("$");
                                if (info[0] == "ganador") 
                                {
                                    //detener todos los hilos
                                    hilo_recibir_carta.Abort();
                                    //enviar mensaje de ganador al cliente
                                    View custom_alert = LayoutInflater.Inflate(Resource.Layout.alerta_ganador, null);
                                    AlertDialog.Builder builder = new AlertDialog.Builder(this);
                                    AlertDialog alert = builder.Create();
                                    alert.SetView(custom_alert);
                                    alert.SetCancelable(false);

                                    //reproducir sonido de ganador
                                    Reproductor.MediaPlayer reproductor_ganador = Reproductor.MediaPlayer.Create(this, Resource.Raw.ganaste);
                                    Reproductor.MediaPlayer reproductor_ganador_musica = Reproductor.MediaPlayer.Create(this, Resource.Raw.win);

                                    reproductor_ganador.Start();
                                    reproductor_ganador_musica.Start();

                                    custom_alert.FindViewById<TextView>(Resource.Id.lbl_nombre_usuario).Text = "Ganaste " + jugador;

                                    custom_alert.FindViewById<Button>(Resource.Id.btn_menu).Click += delegate
                                    {
                                        player_musica_fondo.Stop();
                                        player_musica_busqueda.Stop();
                                        player_musica_juego.Stop();
                                        //guardar bandera para reproducir la musica
                                        ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
                                        prefs = PreferenceManager.GetDefaultSharedPreferences(this);
                                        ISharedPreferencesEditor editor = prefs.Edit();
                                        editor.PutBoolean("pref_back_from_game", true);
                                        editor.Apply();

                                        //detener la musica
                                        reproductor_ganador.Stop();
                                        reproductor_ganador.Release();
                                        reproductor_ganador_musica.Stop();
                                        reproductor_ganador_musica.Release();

                                        this.Finish();
                                    };
                                    alert.Show();
                                }
                                else if(info[0] == "trampa")
                                {
                                    partida_pausada = true;
                                    custom_alert_trampa = LayoutInflater.Inflate(Resource.Layout.alerta_trampa, null);
                                    builder_trampa = new AlertDialog.Builder(this);
                                    alert_trampa = builder_trampa.Create();
                                    alert_trampa.SetView(custom_alert_trampa);
                                    alert_trampa.SetCancelable(false);
                                    var nombre = custom_alert_trampa.FindViewById<EditText>(Resource.Id.txt_jugador_trampa);
                                    nombre.Text = info[1];

                                    //reproducir sonido de trampa
                                    reproductor_trampa = Reproductor.MediaPlayer.Create(this, Resource.Raw.trampa);
                                    reproductor_trampa_musica = Reproductor.MediaPlayer.Create(this, Resource.Raw.cheat);

                                    reproductor_trampa.Start();
                                    reproductor_trampa_musica.Start();

                                    alert_trampa.Show();
                                    await Task.Delay(4000);
                                    alert_trampa.Dismiss();

                                }
                                else
                                {
                                    //enviar mensaje de pededor al cliente
                                    View custom_alert = LayoutInflater.Inflate(Resource.Layout.alerta_perdedor, null);
                                    AlertDialog.Builder builder = new AlertDialog.Builder(this);
                                    AlertDialog alert = builder.Create();
                                    alert.SetView(custom_alert);
                                    alert.SetCancelable(false);

                                    //reproducir sonido de ganador
                                    Reproductor.MediaPlayer reproductor_perdedor = Reproductor.MediaPlayer.Create(this, Resource.Raw.perdiste);
                                    Reproductor.MediaPlayer reproductor_perdedor_musica = Reproductor.MediaPlayer.Create(this, Resource.Raw.fail);

                                    reproductor_perdedor.Start();
                                    reproductor_perdedor_musica.Start();

                                    custom_alert.FindViewById<TextView>(Resource.Id.lbl_nombre_usuario).Text = "Perdiste " + jugador;
                                    custom_alert.FindViewById<TextView>(Resource.Id.lbl_ganador).Text = "Gano: " + info[1];

                                    custom_alert.FindViewById<Button>(Resource.Id.btn_menu).Click += delegate
                                    {
                                        player_musica_fondo.Stop();
                                        player_musica_busqueda.Stop();
                                        player_musica_juego.Stop();
                                        //guardar bandera para reproducir la musica
                                        ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
                                        prefs = PreferenceManager.GetDefaultSharedPreferences(this);
                                        ISharedPreferencesEditor editor = prefs.Edit();
                                        editor.PutBoolean("pref_back_from_game", true);
                                        editor.Apply();

                                        //detener la musica
                                        reproductor_perdedor.Stop();
                                        reproductor_perdedor.Release();
                                        reproductor_perdedor.Stop();
                                        reproductor_perdedor.Release();

                                        this.Finish();
                                    };
                                    alert.Show();
                                }
                                break;
                        }
                    }

                });
            }
        }

        public void conectar(string IP, AlertDialog alert)
        {
            try
            {
                IP_server = IP;
                connect_cliente = new IPEndPoint(IPAddress.Parse(IP), 8000);
                listen_cliente.Connect(connect_cliente);
                byte[] enviar_info = new byte[100];

                enviar_info = Encoding.Default.GetBytes(jugador + "$" + obtenerIP());

                listen_cliente.Send(enviar_info);

                alert.Cancel();

                hilo_recibir_carta = new Thread(recibir_carta);
                hilo_recibir_carta.IsBackground = true;
                hilo_recibir_carta.Start();
            }
            catch
            {
                Toast.MakeText(this, "no se encontro el servidor", ToastLength.Short).Show();
            }
        }

        internal static IPAddress obtenerIP()
        {
            IPAddress ipAddress = null;
            var guardar = false;
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            if (!guardar)
                            {
                                ipAddress = ip.Address;
                                guardar = true;
                                Console.WriteLine(ip.Address);
                            }
                        }
                    }
                }
            }
            return ipAddress;
        }

        //metodos para controlar los hilos si la app ce cierra
        protected override void OnDestroy()
        {
            var cliente = Intent.GetStringExtra("cliente") ?? "no";

            if (cliente == "si")
            {
                //RunOnUiThread(() =>
                //{
                //    reclamar_buenas(true);
                //    Thread.Sleep(2000);
                //});
            }
            else
            {
                //enviar a los clientes que se finalizara la partida
                byte[] mensaje = new byte[100];
                mensaje = Encoding.Default.GetBytes("server_error");

                foreach (var item in Clientes)
                {
                    item.socket_jugador.Send(mensaje);
                }

                //detener todos los hilos
                try
                {
                    hilo_espear_jugadores.Abort();
                    listen_server.Close();
                    hilo_espear_ganador.Abort();
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                }
            }

            base.OnDestroy();
        }

        public bool OnError(Reproductor.MediaPlayer mp, [GeneratedEnum] Reproductor.MediaError what, int extra)
        {
            Console.WriteLine(what);
            return true;
            //throw new NotImplementedException();
        }
    }


    public class jugador_model
    {
        public string nombre_jugador { get; set; }
        public Socket socket_jugador { get; set; }
        public string ip_jugador { get; set; }
    }
}
