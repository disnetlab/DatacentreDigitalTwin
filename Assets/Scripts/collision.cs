using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;
using UnityEngine.Networking;
using System.Text;
using SimpleJSON;

public class collision : MonoBehaviour
{
    public string tokenPub;

    public List<Hypervisors> hypervisorsList = new List<Hypervisors>();
    public List<Servers> serversList = new List<Servers>();

    void Start()
    {
        StartCoroutine(getToken());
    }

    void OnTriggerEnter(Collider other)
    {

        //programCube scriptA = gameObject.AddComponent<programCube>();
        if (other.gameObject.tag == "Plate")
        {
            Debug.Log(gameObject.name + " " + "triggered" + " " + other.gameObject.name);
            string n = gameObject.name;
            string m = other.gameObject.name;
            preMig(n, m);
            StartCoroutine(checkMigForVm(tokenPub, gameObject.name, gameObject));
        }
    }

    public void preMig(string c, string p)
    {
        string cID = "";
        Debug.Log(serversList.Count);
        for (int i = 0; i < serversList.Count; i++)
        {
            if (serversList[i].Name == c)
            {
                cID = serversList[i].Id;
                Debug.Log(cID);
                Debug.Log(p);
                //Migrate(cID, p);
            }
        }
    }

    IEnumerator checkMigForVm(string token, string id, GameObject obj)
    {
        string status2 = "";
        string serverID = "";
        int counter = 9999999;

        for (int i = 0; i < serversList.Count; i++)
        {
            if (serversList[i].Name == id)
            {
                serverID = serversList[i].Id;
            }
        }

        while (counter > 0)
        {
            yield return new WaitForSeconds(0.1f);
            counter--;
            string openstackURL = "http://192.168.0.1:8774/v2.1/servers/" + serverID + "/migrations";
            UnityWebRequest request = UnityWebRequest.Get(openstackURL);
            request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
            request.SetRequestHeader("X-Auth-Token", token);
            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                yield break;
            }

            JSONNode statusInfo = JSON.Parse(request.downloadHandler.text);
            status2 = statusInfo["migrations"]["status"];

            // if (status2 == "completed")
            // {
            //     StartCoroutine(getToken());
            //     break;
            // }

            if (obj.activeSelf)
            {
                obj.SetActive(false);
            }
            else
            {
                obj.SetActive(true);
            }

            if (status2 == "completed")
            {
                obj.SetActive(true);
            }
            else
            {
                obj.SetActive(true);
            }
            break;
        }
        //yield return new WaitForSeconds(3f);
    }

    IEnumerator getToken()
    {
        List<string> values = new List<string>();
        string token = "";
        var jsonString = "{\"auth\":{\"identity\":{\"methods\":[\"password\"],\"password\":{\"user\":{\"id\":\"*TOKEN*\",\"password\":\"*PASSPORD*\"}}},\"scope\":{\"system\":{\"all\":true}}}}";
        byte[] byteData = System.Text.Encoding.ASCII.GetBytes(jsonString.ToCharArray());

        string openstackURL = "http://192.168.0.1:5000/v3/auth/tokens";
        UnityWebRequest unityWebRequest = UnityWebRequest.Post(openstackURL, "POST");
        unityWebRequest.uploadHandler = new UploadHandlerRaw(byteData);
        unityWebRequest.SetRequestHeader("Content-Type", "application/json");
        DownloadHandlerBuffer downloadHandlerBuffer = new DownloadHandlerBuffer();
        unityWebRequest.downloadHandler = downloadHandlerBuffer;
        string response = unityWebRequest.downloadHandler.text;
        yield return unityWebRequest.SendWebRequest();

        if (unityWebRequest.isNetworkError || unityWebRequest.isHttpError)
        {
            yield break;
        }
        else
        {
            StringBuilder sb = new StringBuilder();
            foreach (System.Collections.Generic.KeyValuePair<string, string> dict in unityWebRequest.GetResponseHeaders())
            {
                sb.Append(dict.Key).Append(": \t[").Append(dict.Value).Append("]\n");
                values.Add(dict.Value);
            }
            token = values[2];
            tokenPub = token;
            StartCoroutine(getAllHypervisors(token));
            StartCoroutine(getAllTenants(token));

        }
    }

    IEnumerator Migrate(string serverId, string hostId)
    {
        var jsonString = "{\"os-migrateLive\": {\"host\": \"" + hostId + "\",\"block_migration\": \"auto\",\"force\": false}}";
        byte[] byteData = System.Text.Encoding.ASCII.GetBytes(jsonString.ToCharArray());
        string openstackURL = "http://192.168.0.1:8774/v2.1/servers/" + serverId + "/action";
        UnityWebRequest unityWebRequest = UnityWebRequest.Post(openstackURL, "POST");
        unityWebRequest.uploadHandler = new UploadHandlerRaw(byteData);
        unityWebRequest.SetRequestHeader("Content-Type", "application/json");
        unityWebRequest.SetRequestHeader("X-Auth-Token", tokenPub);
        DownloadHandlerBuffer downloadHandlerBuffer = new DownloadHandlerBuffer();
        unityWebRequest.downloadHandler = downloadHandlerBuffer;
        string response = unityWebRequest.downloadHandler.text;
        yield return unityWebRequest.SendWebRequest();
    }

    IEnumerator getAllTenants(string token)
    {
        string openstackURL = "http://192.168.0.1:8774/v2.1/servers/detail?=&=&all_tenants=1";
        UnityWebRequest request = UnityWebRequest.Get(openstackURL);
        request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
        request.SetRequestHeader("X-Auth-Token", token);
        yield return request.SendWebRequest();

        if (request.isNetworkError || request.isHttpError)
        {
            yield break;
        }

        JSONNode tenantInfo = JSON.Parse(request.downloadHandler.text);
        serversList = new List<Servers>(tenantInfo.Count);

        for (int i = 0; i < tenantInfo["servers"].Count; i++)
        {
            Servers server = new Servers();
            server.FlavorId = tenantInfo["servers"][i]["flavor"]["id"];
            server.Id = tenantInfo["servers"][i]["id"];
            server.Name = tenantInfo["servers"][i]["name"];
            server.HostName = tenantInfo["servers"][i]["OS-EXT-SRV-ATTR:host"];
            server.Status = tenantInfo["servers"][i]["status"];
            server.ProjectId = tenantInfo["servers"][i]["tenant_id"];

            serversList.Add(server);
        }

        int num = 0;
        for (int i = 0; i < serversList.Count; i++)
        {
            if (serversList[i].HostName.Equals("compute8"))
            {
                num++;
            }
        }
    }

    IEnumerator getAllHypervisors(string token)
    {
        string openstackURL = "http://192.168.0.1:8774/v2.1/os-hypervisors/detail";
        UnityWebRequest request = UnityWebRequest.Get(openstackURL);
        request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
        request.SetRequestHeader("X-Auth-Token", token);
        yield return request.SendWebRequest();

        if (request.isNetworkError || request.isHttpError)
        {
            yield break;
        }

        JSONNode hypervisorInfo = JSON.Parse(request.downloadHandler.text);
        hypervisorsList = new List<Hypervisors>(hypervisorInfo.Count);

        for (int i = 0; i < hypervisorInfo["hypervisors"].Count; i++)
        {
            Hypervisors hypervisor = new Hypervisors();
            hypervisor.Status = hypervisorInfo["hypervisors"][i]["status"];
            hypervisor.State = hypervisorInfo["hypervisors"][i]["state"];
            hypervisor.Id = hypervisorInfo["hypervisors"][i]["id"];
            hypervisor.Name = hypervisorInfo["hypervisors"][i]["hypervisor_hostname"];
            hypervisor.NoVms = hypervisorInfo["hypervisors"][i]["running_vms"];
            hypervisorsList.Add(hypervisor);
        }
    }


    public class Hypervisors
    {
        public string State { get; set; }
        public string Status { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public int NoVms { get; set; }

        public Hypervisors()
        {
            Name = "";
            Id = 0;
            State = "";
            Status = "";
            NoVms = 0;
        }

        public Hypervisors(string name, int id, string state, string status, int noVms)
        {
            Name = name;
            Id = id;
            State = state;
            Status = Status;
            NoVms = noVms;
        }
    }

    public class Servers
    {
        public string HostName { get; set; }
        public string Status { get; set; }
        public int FlavorId { get; set; }
        public string Name { get; set; }
        public string ProjectId { get; set; }
        public string Id { get; set; }

        public Servers()
        {

        }

        public Servers(string name, int flavorid, string hostName, string status, string projectId, string id)
        {
            Name = name;
            FlavorId = flavorid;
            HostName = hostName;
            Status = Status;
            ProjectId = projectId;
            Id = id;
        }
    }

}
