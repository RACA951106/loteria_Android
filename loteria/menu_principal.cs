using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Views;
using Android.Widget;
using Android.Media;
using Android.Animation;
using System;
using System.Threading.Tasks;
using Com.Airbnb.Lottie;
using Android.App.Usage;
using Android.Net.Wifi;
using Android.Net;
using Plugin.Connectivity;
using System.Linq;

namespace loteria
{
    [Activity(Label = "Loteria", MainLauncher =true, Theme ="@android:style/Theme.Holo.Light.NoActionBar.Fullscreen")]
    public class menu_principal : Activity
    {
        MediaPlayer player_musica_menu;
        MediaPlayer player_up_sound;
        MediaPlayer player_letrero_sound;
        MediaPlayer player_loteria_sound;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.menu_principal);

            //reproducir la musica principal
            player_musica_menu = MediaPlayer.Create(this, Resource.Raw.menuMusic);
            player_up_sound = MediaPlayer.Create(this, Resource.Raw.upMono);
            player_loteria_sound = MediaPlayer.Create(this, Resource.Raw.LOTERIA);
            player_letrero_sound = MediaPlayer.Create(this, Resource.Raw.loteriaLetrero);

            var btn_crear = FindViewById<Button>(Resource.Id.btn_crear);
            var btn_unirse = FindViewById<Button>(Resource.Id.btn_unirse);
            var txt_usuario = FindViewById<EditText>(Resource.Id.txt_usuario);
            var linearRojo = FindViewById<LinearLayout>(Resource.Id.lineaRoja);
            var mono = FindViewById<ImageView>(Resource.Id.mono);
            var fuegos = FindViewById<LottieAnimationView>(Resource.Id.fuegos_artificiales);
            var letrero = FindViewById<ImageView>(Resource.Id.letrero);

            //animar mono para que salga desde abajo
            var originalPosition = mono.TranslationY;
            //obtener la medida de la pantalla
            var metrics = Resources.DisplayMetrics;

            //if (metrics.HeightPixels < 1200)
            //{
            //    LinearLayout.LayoutParams ll = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.WrapContent, linearRojo.Height);
            //    ll.SetMargins(0, 0, 0, 0);
            //    linearRojo.LayoutParameters = ll;
            //}

            //poner por defecto la medida de la pantalla en la imagen
            mono.TranslationY = metrics.HeightPixels / 3;
            //correr la animacion para que suba el mono
            ObjectAnimator animatorY = ObjectAnimator.OfFloat(mono, "translationY", originalPosition);
            animatorY.SetDuration(3000);
            animatorY.Start();

            player_up_sound.Start();
            player_up_sound.Completion += delegate
            {
                player_letrero_sound.Start();
                //animar el letrero
                ObjectAnimator scaleDownX = ObjectAnimator.OfFloat(letrero, "scaleX", 1f);
                ObjectAnimator scaleDownY = ObjectAnimator.OfFloat(letrero, "scaleY", 1f);
                scaleDownX.SetDuration(200);
                scaleDownY.SetDuration(200);

                AnimatorSet scaleDown = new AnimatorSet();
                scaleDown.PlaySequentially(scaleDownX, scaleDownY);
                scaleDown.Start();
            };
            player_letrero_sound.Completion += delegate
            {
                fuegos.PlayAnimation();
                player_loteria_sound.Start();
                player_musica_menu.Start();
                player_musica_menu.Looping = true;

                ObjectAnimator alpha1 = ObjectAnimator.OfFloat(linearRojo, "alpha", 1);
                alpha1.SetDuration(1000);
                ObjectAnimator alpha2 = ObjectAnimator.OfFloat(btn_crear, "alpha", 1);
                alpha2.SetDuration(1000);
                ObjectAnimator alpha3 = ObjectAnimator.OfFloat(btn_unirse, "alpha", 1);
                alpha3.SetDuration(1000);
                alpha1.Start();
                alpha2.Start();
                alpha3.Start();
            };

            //poner el nombre de usuario si ya existia antes
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            txt_usuario.Text = prefs.GetString("pref_nombre_jugador", "");
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.PutBoolean("pref_back_from_game", false);
            editor.Apply();

            btn_crear.Click += delegate
            {
                //comprobar si esta conectado a una red

                if (chekWiFiConnection())
                {

                    if (!string.IsNullOrEmpty(txt_usuario.Text))
                    {
                        prefs = PreferenceManager.GetDefaultSharedPreferences(this);
                        editor.PutString("pref_nombre_jugador", txt_usuario.Text);
                        // editor.Commit();    // applies changes synchronously on older APIs
                        editor.Apply();        // applies changes asynchronously on newer APIs

                        player_musica_menu.Stop();
                        var activity = new Intent(this, typeof(mesa_de_juego));
                        activity.PutExtra("jugador", txt_usuario.Text);
                        activity.PutExtra("cliente", "no");
                        StartActivity(activity);
                    }
                    else
                        Toast.MakeText(this, "Necesitas un nombre de usuario", ToastLength.Short).Show();
                }
                else
                {
                    Toast.MakeText(this, "Necesitas estar conectado a una red", ToastLength.Short).Show();
                }
            };

            btn_unirse.Click += delegate
            {
                if (!string.IsNullOrEmpty(txt_usuario.Text))
                {
                    prefs = PreferenceManager.GetDefaultSharedPreferences(this);
                    editor.PutString("pref_nombre_jugador", txt_usuario.Text);
                    // editor.Commit();    // applies changes synchronously on older APIs
                    editor.Apply();

                    player_musica_menu.Stop();
                    var activity = new Intent(this, typeof(mesa_de_juego));
                    activity.PutExtra("jugador", txt_usuario.Text);
                    activity.PutExtra("cliente", "si");
                    StartActivity(activity);
                }
                else
                    Toast.MakeText(this, "Necesitas un nombre de usuario", ToastLength.Short).Show();
            };
        }

        public bool chekWiFiConnection()
        {
            var wifi = Plugin.Connectivity.Abstractions.ConnectionType.WiFi;
            var connectionTypes = CrossConnectivity.Current.ConnectionTypes;
            if (!connectionTypes.Contains(wifi))
            {
                //You do not have wifi
                return false;
            }
            return true;
        }

        protected override void OnResume()
        {
            base.OnResume();
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            var back_from_game = prefs.GetBoolean("pref_back_from_game", false);
            if (back_from_game)
            {
                player_musica_menu = MediaPlayer.Create(this, Resource.Raw.menuMusic);
                player_musica_menu.Start();
            }
        }

        protected override void OnRestart()
        {
            base.OnRestart();
        }

        protected override void OnPause()
        {
            base.OnPause();
            player_musica_menu.Pause();
            player_up_sound.Pause();
            player_letrero_sound.Pause();
            player_loteria_sound.Pause();
        }
    }
}
