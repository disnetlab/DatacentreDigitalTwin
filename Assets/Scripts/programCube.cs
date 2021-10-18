using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;
using UnityEngine.Networking;
using System.Text;
using SimpleJSON;
using UnityEngine.UI;

public class programCube : MonoBehaviour
{
    public string arg;
    public GameObject tilePrefab;
    public int planeCount;
    public bool randomColor;

    public bool checkStatus = true;

    public Color[] colorChoices;
    public Color[] colorChoicesPlane;

    public Color eCol;
    public Color[] ranColors;
    public string tokenPub;
    public bool checkComplete;

    public GameObject migObj;
    List<GameObject> gameObjs = new List<GameObject>();
    List<GameObject> gameObjs2 = new List<GameObject>();

    public List<Hypervisors> hypervisorsList = new List<Hypervisors>();
    public List<Flavors> flavorsList = new List<Flavors>();
    public List<Servers> serversList = new List<Servers>();
    public List<Projects> projectsList = new List<Projects>();

    public List<int> EnergyList = new List<int>();

    // Start is called before the first frame update
    void Start()
    {
        QualitySettings.shadows = ShadowQuality.Disable;
        StartCoroutine(getToken());

        //setup up py script
        StartCoroutine(GetEnergy());

    }

        IEnumerator GetEnergy() {
        string URL = "http://192.168.0.1:9000/";
        UnityWebRequest request = UnityWebRequest.Get(URL);
        yield return request.SendWebRequest();

        if (request.isNetworkError || request.isHttpError)
        {
            yield break;
        }

        JSONNode energy = JSON.Parse(request.downloadHandler.text);
        EnergyList = new List<int>(energy.Count - 1);

        for (int i = 1; i < energy.Count; i++)
        {
            EnergyList.Add(energy[i]);
        }
    }

    IEnumerator getToken()
    {
        checkComplete = false;
        List<string> values = new List<string>();
        string token = "";
        var jsonString = "{\"auth\":{\"identity\":{\"methods\":[\"password\"],\"password\":{\"user\":{\"id\":\"*TOKEN*\",\"password\":\"*password*\"}}},\"scope\":{\"system\":{\"all\":true}}}}";
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
            StartCoroutine(getFlavours(token));
            StartCoroutine(getAllTenants(token));
            StartCoroutine(getProjects(token));
            //StartCoroutine(getMigrateStatus(token));
        }
    }

    IEnumerator ShutOffVm(string token, string id)
    {
        var jsonString = "{\"os-stop\":\"null\"}";
        byte[] byteData = System.Text.Encoding.ASCII.GetBytes(jsonString.ToCharArray());
        string openstackURL = "http://192.168.0.1:8774/v2.1/servers/" + id + "/action";
        //Debug.log(openstackURL);
        //Debug.log(jsonString);
        UnityWebRequest unityWebRequest = UnityWebRequest.Post(openstackURL, "POST");
        unityWebRequest.uploadHandler = new UploadHandlerRaw(byteData);
        unityWebRequest.SetRequestHeader("Content-Type", "application/json");
        unityWebRequest.SetRequestHeader("X-Auth-Token", token);
        DownloadHandlerBuffer downloadHandlerBuffer = new DownloadHandlerBuffer();
        unityWebRequest.downloadHandler = downloadHandlerBuffer;
        string response = unityWebRequest.downloadHandler.text;
        yield return unityWebRequest.SendWebRequest();
    }

    void OnTriggerEnter(Collider other)
    {
        //Debug.log(gameObject.name + "triggered" + other.gameObject.name);
    }

    void OnCollisionEnter(Collision col)
    {
        //Debug.log("hit");
        //Debug.log(gameObject.name + "hit" + col.gameObject.name);
    }

    IEnumerator StartVm(string token, string id)
    {

        var jsonString = "{\"os-start\":\"null\"}";
        byte[] byteData = System.Text.Encoding.ASCII.GetBytes(jsonString.ToCharArray());
        string openstackURL = "http://192.168.0.1:8774/v2.1/servers/" + id + "/action";

        UnityWebRequest unityWebRequest = UnityWebRequest.Post(openstackURL, "POST");
        unityWebRequest.uploadHandler = new UploadHandlerRaw(byteData);
        unityWebRequest.SetRequestHeader("Content-Type", "application/json");
        unityWebRequest.SetRequestHeader("X-Auth-Token", token);
        DownloadHandlerBuffer downloadHandlerBuffer = new DownloadHandlerBuffer();
        unityWebRequest.downloadHandler = downloadHandlerBuffer;
        string response = unityWebRequest.downloadHandler.text;
        yield return unityWebRequest.SendWebRequest();
    }

    public void preMig(string c, string p)
    {
        string cID = "";
        //string pId = "";

        for (int i = 0; i < serversList.Count; i++)
        {
            if (serversList[i].Name == c)
            {
                cID = serversList[i].Id;
                //Debug.log(cID);
            }
        }

        //  for (int i = 0; i < hypervisorsList.Count; i++)
        // {
        //     if (hypervisorsList[i].Name == p)
        //     {
        //         pId = hypervisorsList[i].Name;
        //         //Debug.log(pId);
        //     }
        // }

        //StartCoroutine(Migrate(cID, p));

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

    IEnumerator getMigrateStatus(string token)
    {
        string status2 = "";
        string serverID = "";

        int counter = 999999999;
        while (counter > 0)
        {
            yield return new WaitForSeconds(0.1f);
            counter--;
            string openstackURL = "http://192.168.0.1:8774/v2.1/os-migrations";
            UnityWebRequest request = UnityWebRequest.Get(openstackURL);
            request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
            request.SetRequestHeader("X-Auth-Token", token);
            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                yield break;
            }

            JSONNode statusInfo = JSON.Parse(request.downloadHandler.text);

            for (int i = 0; i < statusInfo["migrations"].Count; i++)
            {
                status2 = statusInfo["migrations"][i]["status"];

                if(status2 == "running")
                {
                    serverID = statusInfo["migrations"][i]["instance_uuid"];
                    StartCoroutine(checkMigForVm(tokenPub, serverID));
                    break;
                }
            }
        }
    }

    IEnumerator checkMigForVm(string token, string id)
    {
        string status2 = "";
        int counter = 9999999;



        while (counter > 0)
        {
            yield return new WaitForSeconds(0.1f);
            counter--;
            string openstackURL = "http://192.168.0.1:8774/v2.1/servers/" + id +"/migrations";
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

            if(status2 == "completed")
            {
                StartCoroutine(getToken());
                break;
            }
        }
        //yield return new WaitForSeconds(3f);
    }


    public void DestroyAllGameObjects()
    {
        gameObjs2 = new List<GameObject>(gameObjs);
        foreach (GameObject gameObj in gameObjs2)
        {
            Destroy(gameObj);
            //gameObj.SetActive(false);
        }
        StartCoroutine(Countdown());
        StartCoroutine(getToken());
        StartCoroutine(GetEnergy());

    }

    IEnumerator Countdown()
    {
        int counter = 5;
        while (counter > 0)
        {
            yield return new WaitForSeconds(1);
            counter--;
        }
        StartCoroutine(getToken());
        //yield return new WaitForSeconds(3.5f);

        if (checkComplete)
        {
           DestroyAllGameObjects();
        }

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
        makeCube();
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

        //sort the list by name 1 - 8
        hypervisorsList.Sort((x, y) => y.Name.CompareTo(x.Name));
    }

    IEnumerator getFlavours(string token)
    {
        string openstackURL = "http://192.168.0.1:8774/v2.1/flavors";
        UnityWebRequest request = UnityWebRequest.Get(openstackURL);
        request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
        request.SetRequestHeader("X-Auth-Token", token);
        yield return request.SendWebRequest();

        if (request.isNetworkError || request.isHttpError)
        {
            yield break;
        }

        JSONNode flavourInfo = JSON.Parse(request.downloadHandler.text);

        flavorsList = new List<Flavors>(flavourInfo.Count);

        for (int i = 0; i < flavourInfo["flavors"].Count; i++)
        {
            Flavors flavor = new Flavors();
            flavor.Id = flavourInfo["flavors"][i]["id"];
            flavor.Name = flavourInfo["flavors"][i]["name"];

            flavorsList.Add(flavor);
        }
    }

    IEnumerator getVmStatus(string token, string id, string status, GameObject obj)
    {
        string status2 = "";

        int counter = 9999999;

        checkStatus = true;

        while (counter > 0)
        {
            yield return new WaitForSeconds(0.1f);
            counter--;
            string openstackURL = "http://192.168.0.1:8774/v2.1//servers/" + id;
            UnityWebRequest request = UnityWebRequest.Get(openstackURL);
            request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
            request.SetRequestHeader("X-Auth-Token", token);
            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                yield break;
            }

            JSONNode statusInfo = JSON.Parse(request.downloadHandler.text);
            status2 = statusInfo["server"]["status"];

            if (obj.activeSelf)
            {
                obj.SetActive(false);
            }
            else
            {
                obj.SetActive(true);
            }

            if (status != status2)
            {
                if (status2 == "ACTIVE")
                {
                    obj.SetActive(true);
                }
                else
                {
                    obj.SetActive(false);
                }
                break;
            }
        }

        checkStatus = false;
        StartCoroutine(getToken());
        //yield return new WaitForSeconds(3f);
    }

    IEnumerator getProjects(string token)
    {
        string openstackURL = "http://192.168.0.1:5000/v3/projects";
        UnityWebRequest request = UnityWebRequest.Get(openstackURL);
        request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
        request.SetRequestHeader("X-Auth-Token", token);
        yield return request.SendWebRequest();

        if (request.isNetworkError || request.isHttpError)
        {
            yield break;
        }

        JSONNode projectInfo = JSON.Parse(request.downloadHandler.text);
        projectsList = new List<Projects>(projectInfo.Count);

        for (int i = 0; i < projectInfo["projects"].Count; i++)
        {
            Projects project = new Projects();
            project.Id = projectInfo["projects"][i]["id"];
            project.Name = projectInfo["projects"][i]["name"];
            project.CubeColor = EcolorList[i];
            // Debug.Log(EcolorList[i]);
            // Debug.Log(i);
            //RandomColor(1, 1);
            projectsList.Add(project);
        }
        ranColors = new Color[projectsList.Count];
    }

    List<Color> EcolorList = new List<Color>()
 {
     Color.red,
     Color.green,
     Color.yellow,
     Color.magenta,
     new Color (0, 1, 1, 1),
     new Color (1, (float)0.92, (float)0.016, 1),
     new Color (0, 1, 1, 1),
     new Color (0, 1, 1, 1)
 };

    public Color getColor(int rgb)
    {
        float max = 320;

        if (rgb > max)
        {
            rgb = (int)max;
        }

        float grad = rgb / max;

        if(grad < 0.5 || grad == 0.5)
        {
            grad = grad/((float)0.5);
            eCol = new Color((grad) * 1, 0,1);
        } else {
             grad = (1 - grad)/((float)0.5);
             eCol = new Color( 1, 0, (grad) * 1);
        }
        return eCol;
    }

    public void makeCube()
    {
            DestroyAllGameObjects();

        int num;
        int n = 0;
        serversList.Sort((x, y) => x.FlavorId.CompareTo(y.FlavorId));
        serversList.Reverse();

        List<Servers> newServersList = new List<Servers>(serversList);
        List<Servers> newServersList2 = new List<Servers>(serversList);
        for (int i = 0; i < hypervisorsList.Count; i++)
        {
            n++;
            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.GetComponent<Collider>().isTrigger = true;
            Rigidbody plateRigidBody = plate.AddComponent<Rigidbody>();
            plate.tag = "Plate";
            plateRigidBody.isKinematic = true;
            plateRigidBody.detectCollisions = true;
            plateRigidBody.useGravity = false;
            gameObjs.Add(plate);

            //  GameObject gameObject = new GameObject("Child");
            //   gameObject.transform.SetParent(this.transform);
            //   gameObject .AddComponent<Text>().text = "Hello This is Child";
            //   gameObject .GetComponent<Text>().font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;

            // text label code
            //   GameObject text = new GameObject();
            //   TextMesh t = text.AddComponent<TextMesh>();
            //   t.text = "new text set";
            //  t.fontSize = 30;
            //  t.transform.localEulerAngles += new Vector3(90, 0, 0);
            //  t.transform.localPosition += new Vector3(56f, 3f, 40f);

            // if (randomColor)
            // {
            //     plate.GetComponent<Renderer>().material.color = colorChoicesPlane[Random.Range(0, (colorChoicesPlane.Length))];
            // }
             if (hypervisorsList[i].Name.Equals("compute1"))
             {
                // Debug.Log(EnergyList[0]);
                // Debug.Log("color");
                plate.GetComponent<Renderer>().material.color = getColor(EnergyList[0]);
                //
             }

              if (hypervisorsList[i].Name.Equals("compute2"))
             {
                // Debug.Log(EnergyList[1]);
                // Debug.Log("color");
                plate.GetComponent<Renderer>().material.color = getColor(EnergyList[1]);
             }

              if (hypervisorsList[i].Name.Equals("compute3"))
             {
                // Debug.Log(EnergyList[2]);
                // Debug.Log("color");
                plate.GetComponent<Renderer>().material.color = getColor(EnergyList[2]);
             }

              if (hypervisorsList[i].Name.Equals("compute4"))
             {
                // Debug.Log(EnergyList[3]);
                // Debug.Log("color");
                plate.GetComponent<Renderer>().material.color = getColor(EnergyList[3]);
             }

              if (hypervisorsList[i].Name.Equals("compute5"))
             {
                // Debug.Log(EnergyList[4]);
                // Debug.Log("color");
                plate.GetComponent<Renderer>().material.color = getColor(EnergyList[4]);
             }

              if (hypervisorsList[i].Name.Equals("compute6"))
             {
                // Debug.Log(EnergyList[5]);
                // Debug.Log("color");
                plate.GetComponent<Renderer>().material.color = getColor(EnergyList[5]);
             }

              if (hypervisorsList[i].Name.Equals("compute7"))
             {
                // Debug.Log(EnergyList[6]);
                // Debug.Log("color");
                plate.GetComponent<Renderer>().material.color = getColor(EnergyList[6]);
             }

              if (hypervisorsList[i].Name.Equals("compute8"))
             {
                // Debug.Log(EnergyList[7]);
                // Debug.Log("color");
                plate.GetComponent<Renderer>().material.color = getColor(EnergyList[7]);
             }

            if (hypervisorsList[i].State.Equals("down"))
            {
                Material m = plate.GetComponent<Renderer>().material;
                m.SetFloat("_Mode", 3);
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 1);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.renderQueue = -1;
                plate.GetComponent<MeshRenderer>().material.color = new Color(m.color.r, m.color.g, m.color.b, 0.2f);
                Renderer rend = plate.GetComponent<Renderer>();
                Shader shader = Shader.Find("Transparent/Diffuse");
                rend.material.shader = shader;
                //plate.GetComponent<MeshRenderer>().material.color = new Color(0, 0, 0, 0.5f);
            }
            num = hypervisorsList[i].NoVms;
            int p = 1;
            var x = 1.85f;

            var totalScaleNow = 0f;
            var zsize = 0.9f;
            var cubeIncrement = 0;
            int j = 1;
            int startingFlavorSize = 990;

            for (int a = 0; a < newServersList.Count; a++)
            {

                if (hypervisorsList[i].Name == newServersList[a].HostName)
                {
                    cubeIncrement = cubeIncrement + 1;
                    //Debug.log(newServersList[a].FlavorId.ToString());
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.GetComponent<Collider>().isTrigger = true;
                    Rigidbody cubeRigidBody = cube.AddComponent<Rigidbody>();
                    cube.AddComponent<collision>();
                    cube.tag = "Cube";
                    cubeRigidBody.isKinematic = true;
                    cubeRigidBody.detectCollisions = true;
                    cubeRigidBody.useGravity = false;
                    gameObjs.Add(cube);
                    gameObjs2.Add(cube);
                    cube.name = newServersList[a].Name;
                    var nsize = 0f;
                    if (newServersList[a].FlavorId == 5)
                    {
                        cube.transform.localScale = new Vector3(3.2f, 1.6f, 1.6f);
                        nsize = n + 0.3f;
                        totalScaleNow = totalScaleNow + 3.2f;
                    }

                    if (serversList[a].FlavorId == 4)
                    {
                        cube.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
                        nsize = n + 0.3f;
                        totalScaleNow = totalScaleNow + 1.6f;
                    }

                    if (serversList[a].FlavorId == 3)
                    {
                        cube.transform.localScale = new Vector3(1.6f, 0.8f, 0.8f);
                        nsize = n;
                        totalScaleNow = totalScaleNow + 1.6f;
                    }

                    if (serversList[a].FlavorId == 2)
                    {
                        cube.transform.localScale = new Vector3(0.8f, 0.55f, 0.8f);
                        nsize = n;
                        totalScaleNow = totalScaleNow + 0.8f;
                    }

                    if (serversList[a].FlavorId == 1)
                    {
                        cube.transform.localScale = new Vector3(0.8f, 0.2f, 0.8f);
                        nsize = n;
                        totalScaleNow = totalScaleNow + 0.8f;
                    }

                    if (totalScaleNow >= 6)
                    {
                        zsize = zsize - 1.8f;
                        x = 1.85f;
                        totalScaleNow = 0;
                        cubeIncrement = 0;
                    }

                    if (cubeIncrement != 0)
                    {
                        if (startingFlavorSize == 5)
                        {
                            if (serversList[a].FlavorId == 5)
                            {
                                x = x - 4;
                            }

                            if (serversList[a].FlavorId == 4)
                            {
                                x = x - 3;
                            }

                            if (serversList[a].FlavorId == 3)
                            {
                                x = x - 4;
                            }

                            if (serversList[a].FlavorId == 2)
                            {
                                x = x - 5;
                            }

                        }

                        if (startingFlavorSize == 4)
                        {
                            if (serversList[a].FlavorId == 4)
                            {
                                x = x - 2;
                            }

                            if (serversList[a].FlavorId == 3)
                            {
                                x = x - 3;
                            }

                            if (serversList[a].FlavorId == 2)
                            {
                                x = x - 4;
                            }
                        }

                        if (startingFlavorSize == 3)
                        {
                            if (serversList[a].FlavorId == 3)
                            {
                                x = x - 3;
                            }

                            if (serversList[a].FlavorId == 2)
                            {
                                x = x - 4;
                            }
                        }

                        if (startingFlavorSize == 2)
                        {
                            if (serversList[a].FlavorId == 2)
                            {
                                x = x - 4;
                            }
                        }
                    }
                    startingFlavorSize = serversList[a].FlavorId;

                    cube.transform.position = new Vector3(x, i + nsize, zsize);

                    //cube.transform.position = new Vector3(x, i + n, z);

                    //change the color of the vms, based on the project
                    for (int q = 0; q < projectsList.Count; q++)
                    {
                        ////Debug.log(projectsList.Count);
                        if (serversList[a].ProjectId == projectsList[q].Id)
                        {
                            cube.GetComponent<Renderer>().material.color = projectsList[q].CubeColor;
                        }
                    }



                    //make cubes transparent
                    if (newServersList[a].Status.Equals("SHUTOFF") || newServersList[a].Status.Equals("SUSPENDED"))
                    {
                        Material m = cube.GetComponent<Renderer>().material;
                        m.SetFloat("_Mode", 3);
                        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        m.SetInt("_ZWrite", 1);
                        m.DisableKeyword("_ALPHATEST_ON");
                        m.EnableKeyword("_ALPHABLEND_ON");
                        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        m.renderQueue = -1;
                        cube.GetComponent<MeshRenderer>().material.color = new Color(m.color.r, m.color.g, m.color.b, 0.2f);

                        Renderer rend = cube.GetComponent<Renderer>();
                        Shader shader = Shader.Find("Transparent/Diffuse");
                        rend.material.shader = shader;
                        //black cube
                        //cube.GetComponent<MeshRenderer>().material.color = new Color(0, 0, 0, 0.5f);
                    }
                    j++;
                }
            }
            p++;
            plate.transform.position = new Vector3(-0.9f, i + n - 0.7f, 0);
            plate.transform.localScale = new Vector3(9, 0.3f, 4);
            plate.name = hypervisorsList[i].Name;
            checkComplete = true;
        }
    }

    public static Color RandomColor(float s, float v)
    {
        var hue = Random.Range(0f, 1f);
        return Color.HSVToRGB(hue, s, v);
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

    public class Flavors
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public Flavors()
        {

        }

        public Flavors(string name, int id)
        {
            Name = name;
            Id = id;
        }
    }

    public class Projects
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Color CubeColor { get; set; }

        public Projects()
        {

        }

        public Projects(string name, string id, Color cubeColor)
        {
            Name = name;
            Id = id;
            CubeColor = cubeColor;
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

    // Update is called once per frame
    public float speed = 5.0f;
    public float zoomspeed = 5.0f;

    public float clickTimer = 0.0f;

    private bool _mouseState;
    private GameObject target;
    public Vector3 screenSpace;
    public Vector3 offset;

    void Update()
    {
        if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.Translate(new Vector3(speed * Time.deltaTime, 0, 0));
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Translate(new Vector3(-speed * Time.deltaTime, 0, 0));
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            transform.Translate(new Vector3(0, -speed * Time.deltaTime, 0));
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            transform.Translate(new Vector3(0, speed * Time.deltaTime, 0));
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.position += transform.forward * zoomspeed * Time.deltaTime;

        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.position -= transform.forward * zoomspeed * Time.deltaTime;
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (Input.GetMouseButton(1))
            {
                clickTimer += Time.deltaTime;
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                //Debug.log(clickTimer += Time.deltaTime);

                if (clickTimer > 0.0f)
                {
                    if (Physics.Raycast(ray, out hit))
                    {
                        //Debug.log(hit.transform.name);
                        for (int i = 0; i < serversList.Count; i++)
                        {
                            if (serversList[i].Name == hit.transform.name)
                            {
                                if (serversList[i].Status.Equals("SHUTOFF") || serversList[i].Status.Equals("SUSPENDED"))
                                {
                                    Debug.Log("on");
                                    StartCoroutine(StartVm(tokenPub, serversList[i].Id));
                                    //Debug.log(tokenPub);
                                    //Debug.log(serversList[i].Id);
                                    //DestroyAllGameObjects();
                                    //StartCoroutine(Countdown());
                                    StartCoroutine(getVmStatus(tokenPub, serversList[i].Id, serversList[i].Status, hit.transform.gameObject));
                                }
                                else
                                {
                                    Debug.Log("off");
                                    StartCoroutine(ShutOffVm(tokenPub, serversList[i].Id));
                                    //Debug.log(tokenPub);
                                    //Debug.log(serversList[i].Id);
                                    //DestroyAllGameObjects();
                                    //StartCoroutine(Countdown());
                                    StartCoroutine(getVmStatus(tokenPub, serversList[i].Id, serversList[i].Status, hit.transform.gameObject));


                                }
                            }
                        }
                    }
                }
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hitInfo;
            target = GetClickedObject(out hitInfo);
            for (int i = 0; i < serversList.Count; i++)
            {
                if (serversList[i].Name == target.transform.name)
                {
                    if (target != null)
                    {
                        _mouseState = true;
                        screenSpace = Camera.main.WorldToScreenPoint(target.transform.position);
                        offset = target.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenSpace.z));
                    }
                }
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            _mouseState = false;
        }
        if (_mouseState)
        {
            //keep track of the mouse position
            var curScreenSpace = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenSpace.z);

            //convert the screen mouse position to world point and adjust with offset
            var curPosition = Camera.main.ScreenToWorldPoint(curScreenSpace) + offset;

            //update the position of the object in the world
            target.transform.position = curPosition;
        }

        GameObject GetClickedObject(out RaycastHit hit)
        {
            GameObject target = null;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray.origin, ray.direction * 10, out hit))
            {
                target = hit.collider.gameObject;
            }

            return target;
        }
    }
}
