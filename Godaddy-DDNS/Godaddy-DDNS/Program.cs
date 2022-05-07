using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;


namespace Godaddy_DDNS
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // domain, hostname, api_key, api_secret
                
                string domain = args[0];
                string hostname = args[1];
                
                string api_key = args[2];
                string api_secret = args[3];
                //Console.WriteLine(domain + "\t" + hostname + "\t" + api_key + "\t" + api_secret);
                
                var ip_now = Get_Local_IP();
                string godaddy_api = $"https://api.godaddy.com/v1/domains/{domain}/records/A/{hostname}";
                var godaddy_ip = Get_GoDaddy_IP(godaddy_api,api_key,api_secret);
                Console.WriteLine($"The A record of Godaddy of {hostname}.{domain} is {godaddy_ip.Result.ToString()}");
                godaddy_ip.Wait();
                ip_now.Wait();
                
                if(!ip_now.Result.ToString().Equals(godaddy_ip.Result.ToString()))
                {
                    Update_DNS(godaddy_api, ip_now.Result.ToString(),api_key,api_secret);
                }
            }
            catch(IndexOutOfRangeException ex)
            {
                Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ex.GetType() + "Message: " + ex.Message);
                Console.WriteLine("The numbers of Parameters are not correct, please check it");
            }
            catch(InvalidOperationException ex)
            {
                Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ex.GetType() + "Message: " + ex.Message);
            }
            catch(NotSupportedException ex)
            {
                Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ex.GetType() + "Message: " + ex.Message);
            }
            catch(Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ex.GetType() + "Message: " + ex.Message);
            }
            
        }

        static async Task<string> Get_Local_IP()
        {
            using (var httpClient = new HttpClient())
            {
                var ip = await httpClient.GetStringAsync("https://api.ipify.org");
                Console.WriteLine($"The local IP is {ip}");
                return ip;
            }
        }

        static async Task<string> Get_GoDaddy_IP(string url,string api_key,string api_secret)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("sso-key", $"{api_key}:{api_secret}");
                HttpResponseMessage response = await client.GetAsync(url);
                if(response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var list = JsonConvert.DeserializeObject<List<Class.GD_DNS>>(result);
                    if(list.Count > 0)
                    {
                        return list[0].data;
                    }
                    else
                    {
                        return "";
                    }
                }
                else
                {
                    Console.WriteLine(response.ReasonPhrase);
                    return "";
                }
            }
        }
        static async void Update_DNS(string url,string ipaddr,string api_key,string api_secret)
        {
            using(var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("sso-key", $"{api_key}:{api_secret}");
                var data = new Class.GD_DNS { data = ipaddr, name = "mvp2", ttl = 60, type = "A" };
                List<Class.GD_DNS> list = new List<Class.GD_DNS>();
                list.Add(data);
                using(var content = new StringContent("[{\"data\":\"" + ipaddr + "\"}]",Encoding.UTF8,"application/json"))
                {
                    var response = client.PutAsync(url, content);
                    response.Wait();
                    if(response.Result.IsSuccessStatusCode)
                    {
                        var result = await response.Result.Content.ReadAsStringAsync();
                        Console.WriteLine($"A record updated to {ipaddr}");
                    }
                    else
                    {
                        Console.WriteLine("A record updated failed");
                    }
                }
                
            }
        }
    }
}
