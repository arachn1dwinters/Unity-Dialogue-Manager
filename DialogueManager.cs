using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using static DialogueManager;
using UnityEngine.Rendering.VirtualTexturing;

public class DialogueManager : EditorWindow
{
    public List<Conversation> conversations = new();
    List<string> conversationNames = new();
    int newConvId = 0;
    bool canCreateNewConv = true;

    public List<string> characters = new();

    int currentConversationID;
    Conversation currentConversation;

    string newConversationName = "New Conversation Name Here";

    GUIStyle textFieldStyle;

    //Colors
    public Color32 lightShades = new(245, 242, 244, 255);
    public Color32 darkAccents = new(121, 124, 123, 255);
    public Color32 lightAccents = new(99, 178, 202, 255);
    public Color32 mainColor = new(60, 110, 150, 255);
    public Color32 darkShades = new(30, 35, 47, 255);

    Texture2D nodeImage = null;
    byte[] nodeImageData;
    Texture2D nodeImageSelected = null;
    byte[] nodeImageSelectedData;
    Texture2D nodeImageCreation = null;
    byte[] nodeImageCreationData;

    bool showNodeName;

    bool changed;
    int currentConversationIdChecker = 0;

    public float screenWidth;

    bool addingCharacter;

    string characterToAdd = "";
    bool canAddCharacter;

    Node nodeToSet;

    GUIStyle nodeStyle;

    public List<Node> allNodes = new();

    [MenuItem("Custom Tools/Dialogue Manager")]
    private static void OpenWindow()
    {
        DialogueManager window = GetWindow<DialogueManager>();
        window.titleContent = new GUIContent("Dialogue Manager");
    }

    void OnEnable()
    {
        if (File.Exists("Assets/Editor/dialogueNode.png")) 
        {
            nodeImageData = File.ReadAllBytes("Assets/Editor/dialogueNode.png");
            nodeImage = new Texture2D(2, 2);
            nodeImage.LoadImage(nodeImageData);
        }

        if (File.Exists("Assets/Editor/dialogueNodeSelected.png"))
        {
            nodeImageSelectedData = File.ReadAllBytes("Assets/Editor/dialogueNodeSelected.png");
            nodeImageSelected = new Texture2D(2, 2);
            nodeImageSelected.LoadImage(nodeImageSelectedData);
        }

        if (File.Exists("Assets/Editor/dialogueNodeCreation.png"))
        {
            nodeImageCreationData = File.ReadAllBytes("Assets/Editor/dialogueNodeCreation.png");
            nodeImageCreation = new Texture2D(2, 2);
            nodeImageCreation.LoadImage(nodeImageCreationData);
        }

        screenWidth = position.width;

        nodeStyle = new();
        nodeStyle.normal.background = nodeImage;
        nodeStyle.normal.textColor = lightShades;
        nodeStyle.border = new RectOffset(12, 12, 12, 12);
        nodeStyle.alignment = TextAnchor.MiddleCenter;

        Load();
    }

    private void OnGUI()
    {
        GUI.DrawTexture(new Rect(0, 0, maxSize.y, maxSize.x), MakeTex(2, 2, mainColor), ScaleMode.StretchToFill);

        if (currentConversation != null)
        {
            currentConversation.DrawNodes();

            conversationNames.Clear();
            foreach (Conversation conversation in conversations)
            {
                conversationNames.Add(conversation.name);
            }

            currentConversationID = EditorGUILayout.Popup(currentConversationID, conversationNames.ToArray());
            if (currentConversationIdChecker != currentConversationID)
            {
                changed = true;
            }

            foreach (Conversation conversation in conversations)
            {
                if (conversation.id == currentConversationID)
                {
                    currentConversation = conversation;
                    currentConversationIdChecker = currentConversation.id;
                    if (changed)
                    {
                        foreach (Node node in currentConversation.nodes)
                        {
                            node.Deselect();
                            node.style.normal.background = nodeImage;
                        }
                        changed = false;
                    }
                    break;
                }
            }
        }

        if (GUI.Button(new Rect((position.width - 175) / 2, 25, 200, 25), "Delete Active Conversation"))
        {
            conversations.Remove(currentConversation);
            newConvId--;

            foreach (Conversation conversation in conversations)
            {
                if (conversation.id > currentConversation.id)
                {
                    conversation.id--;
                }
            }

            if (conversations.Count() != 0)
            {
                currentConversation = conversations.Last();
                currentConversationID = currentConversation.id;
            } else
            {
                currentConversation = null;
            }
        }

        newConversationName = GUI.TextField(new Rect((position.width - 175) / 2, 60, 200, 25), newConversationName, textFieldStyle);

        if (GUI.Button(new Rect((position.width - 175) / 2, 95, 200, 25), "Create Conversation"))
        {
            if (newConversationName != "New Conversation Name Here" || newConversationName != "")
            {
                foreach (Conversation conversation in conversations)
                {
                    if (conversation.name == newConversationName)
                    {
                        canCreateNewConv = false;
                        break;
                    }
                }

                if (canCreateNewConv)
                {
                    Conversation newConv = new(newConvId, newConversationName, true, this);
                    currentConversation = newConv;
                    newConversationName = "New Conversation Name Here";
                    newConvId++;
                    conversations.Add(newConv);
                    currentConversation = newConv;
                    currentConversationID = newConv.id;
                }

                canCreateNewConv = true;
            }
        }

        textFieldStyle = new GUIStyle(GUI.skin.textField);
        textFieldStyle.alignment = TextAnchor.MiddleCenter;

        Event e = Event.current;

        if (currentConversation != null)
        {
            currentConversation.ProcessEvents(e);

            foreach (Node node in currentConversation.nodes)
            {
                if (node.rect.Contains(e.mousePosition))
                {
                    showNodeName = true;
                }
                else
                {
                    showNodeName = false;
                }

                if (showNodeName)
                {
                    GUI.Label(new Rect(20, 10, 300, 50), new GUIContent("Right click for options"));
                    GUI.Label(new Rect(20, 40, 300, 50), new GUIContent("Press enter while selected to rename"));
                    GUI.Label(new Rect(20, 70, 300, 50), new GUIContent("Press \"e\" while selected to view/edit content"));
                    GUI.Label(new Rect(20, 100, 370, 50), new GUIContent("Press \"/\" while selected to view character list or add a character"));
                }

                if (node.renaming)
                {
                    node.title = GUI.TextField(new Rect(node.rect.position.x + 50, node.rect.position.y + 12.5f, 100, 25), node.title, 14, textFieldStyle);
                } else if (node.isCreating)
                {
                    node.style.normal.background = nodeImageCreation;

                    float newX = e.mousePosition.x - 100;
                    float newY = e.mousePosition.y - 25;

                    node.rect.position = new Vector2(newX, newY);
                } else if (node.showingContent)
                {
                    node.content = GUI.TextArea(new Rect((position.width - 300) / 2, 100, 300, 300), node.content);
                }

                foreach (Node i in node.children) {
                    Handles.DrawLine(new Vector2(node.rect.position.x + 100, node.rect.position.y + 50), new Vector2(i.rect.position.x + 100, i.rect.position.y));
                }

                switch (e.type)
                {
                    case EventType.KeyDown:
                        if (e.keyCode == KeyCode.Slash && !addingCharacter && node.selected)
                        {
                            ProcessNodeCharacterList(node);
                            characterToAdd = "";
                        }

                        if (e.keyCode == KeyCode.Return && addingCharacter)
                        {
                            canAddCharacter = true;

                            foreach (string character in characters)
                            {
                                if (characterToAdd == character)
                                {
                                    canAddCharacter = false;
                                    Debug.LogError("This character was already added!");
                                    break;
                                }
                            }

                            if (canAddCharacter)
                            {
                                addingCharacter = false;
                                currentConversation.parentIsaddingCharacter = false;
                                if (characterToAdd != "")
                                {
                                    characters.Add(characterToAdd);
                                }
                                characterToAdd = "";
                            }
                        }
                        break;
                }
            }
        }

        currentConversation?.ProcessNodeEvents(e);

        if (addingCharacter)
        {
            characterToAdd = GUI.TextField(new Rect((position.width / 2) - 100, (position.height  / 2) - 12.5f, 200, 25), characterToAdd, 20, textFieldStyle);
        }

        Repaint();
    }

    void ProcessNodeCharacterList(Node node)
    {
        GenericMenu genericMenu = new();

        characterToAdd = "";

        genericMenu.AddDisabledItem(new GUIContent("Set character for " + node.title), false);
        genericMenu.AddSeparator("");
        if (characters.Count != 0)
        {
            foreach (string character in characters)
            {
                if (character != node.character)
                {
                    genericMenu.AddItem(new GUIContent(character), false, SetNodeCharacter, character);
                } else
                {
                    genericMenu.AddItem(new GUIContent(character), true, SetNodeCharacter, character);
                }
            }
        }
        genericMenu.AddSeparator("");

        if (characters.Count != 0)
        {
            foreach (string character in characters)
            {
                genericMenu.AddItem(new GUIContent("Delete character/" + character), false, DeleteNodeCharacter, character);
            }
        }

        genericMenu.AddSeparator("");
        genericMenu.AddItem(new GUIContent("Add new character"), false, AddCharacter);

        genericMenu.ShowAsContext();
    }

    private void SetNodeCharacter(object characterObj)
    {
        string character = (string)characterObj;

        foreach (Node node in currentConversation.nodes)
        {
            if (node.selected)
            {
                nodeToSet = node;
                break;
            }
        }

        if (nodeToSet != null)
        {
            nodeToSet.character = character;
        }

        nodeToSet = null;
    }

    private void DeleteNodeCharacter(object characterObj)
    {
        string character = (string)characterObj;

        characters.Remove(character);
    }

    private void AddCharacter()
    {
        characterToAdd = "";
        addingCharacter = true;
        currentConversation.parentIsaddingCharacter = true;
    }

    public Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);

        result.Apply();
        return result;
    }

    struct SaveObject
    {
        public List<string> conversations;
        public List<string> characters;
    }

    void Save()
    {
        SaveObject saveObject = new()
        {
            conversations = new(),
            characters = new()
        };

        foreach (Conversation conversation in conversations)
        {
            string stringToSave = conversation.name + ": " + "[" + OutputNodeList(conversation.rootNode.ListToSave()) + "]";

            saveObject.conversations.Add(stringToSave);
        }

        foreach (string character in characters)
        {
            saveObject.characters.Add(character);
        }

        string json = JsonUtility.ToJson(saveObject);
        File.WriteAllText("Assets/Editor/dialogueData.txt", json);
    }

    string OutputNodeList(List<object> nodeList)
    {
        string stringToOutput = "";

        foreach (object obj in nodeList)
        {
            if (obj is string)
            {
                stringToOutput += obj;
                if (nodeList.IndexOf(obj) != nodeList.Count() - 1)
                {
                    stringToOutput += ",";
                }
            }
        }

        if (nodeList.Count() == 4)
        {
            stringToOutput += "[";

            List<List<object>> children = (List<List<object>>)nodeList[3];
            foreach (List<object> child in children)
            {
                stringToOutput += OutputNodeList(child);

                if (children.IndexOf(child) != children.Count() - 1)
                {
                    stringToOutput += "]\\endChild[";
                }
            }

            stringToOutput += "]";
        }
        return stringToOutput;
    }

    void Load()
    {
        string dataString = File.ReadAllText("Assets/Editor/dialogueData.txt");
        SaveObject data = JsonUtility.FromJson<SaveObject>(dataString);
        newConvId = 0;

        foreach (string conversation in data.conversations)
        {
            string[] nameAndRootNode = conversation.Split(": ");

            Conversation newConv = new(newConvId, nameAndRootNode[0], false, this);
            conversations.Add(newConv);
            currentConversation = newConv;
            currentConversationID = newConv .id;
            newConvId++;

            string nodeStringWithoutBrackets = nameAndRootNode[1][1..^1];
            NodeFromString(nodeStringWithoutBrackets, newConv);
        }
    }

    // Call this with a node string WITHOUT square brackets: to remove the first and last chars use the range operator (string[1..^1])
    void NodeFromString(string nodeString, Conversation parentConversation, Node parentNode = null)
    {
        char[] dataChars = nodeString.ToCharArray();
        string dataString = "";
        string childrenString = "";

        foreach (char dataChar in dataChars)
        {
            if (dataChar != '[')
            {
                dataString += dataChar;
            } else
            {
                break;
            }
        }
        childrenString = nodeString.Replace(dataString, "");

        string[] dataWithoutChildren = dataString.Split(",");
        string posDataString = dataWithoutChildren[2][1..^1];
        string[] posData = posDataString.Split("\\endX");
        float x = float.Parse(posData[0]);
        float y = float.Parse(posData[1]);

        Node newNode = new(new Vector2(x, y), 200, 50, nodeStyle, parentConversation, true, parentNode);
        newNode.title = dataWithoutChildren[0];
        newNode.content = dataWithoutChildren[1];
        parentConversation.nodes.Add(newNode);
        allNodes.Add(newNode);

        if (childrenString != "")
        {
            childrenString = childrenString[1..^1];
            string[] children = childrenString.Split("\\endChild");
            foreach (string child in children)
            {
                NodeFromString(child, parentConversation, newNode);
            }
        }

        if (parentNode == null)
        {
            parentConversation.rootNode = newNode;
        } else
        {
            parentNode.children.Add(newNode);
        }
    }

    // Dialogue classes
    public class Conversation
    {
        public int id;
        public string name;
        public List<Node> nodes = new List<Node>();
        GUIStyle nodeStyle;

        //Color
        public Color32 lightShades;
        Color32 lightAccents;
        Color32 mainColor;
        Color32 darkAccents;
        Color32 darkShades;
        public Texture2D nodeBackground;
        Texture2D nodeBackgroundSelected;

        public Rect contentRect;

        bool canDrag;

        List<Node> mouseOverNodes = new();
        Node nodeToSelect;
        bool otherNodeIsSelected;

        public bool parentIsaddingCharacter;

        public DialogueManager parent;

        public Node rootNode;

        public Conversation(int id, string name, bool newConversation, DialogueManager parent)
        {
            this.lightShades = parent.lightShades;
            this.lightAccents = parent.lightAccents;
            this.mainColor = parent.mainColor;
            this.darkAccents = parent.darkAccents;
            this.darkShades = parent.darkShades;
            this.nodeBackground = parent.nodeImage;
            this.nodeBackgroundSelected = parent.nodeImageSelected;
            this.parent = parent;

            nodeStyle = new GUIStyle();
            nodeStyle.normal.background = nodeBackground;
            nodeStyle.normal.textColor = this.lightShades;
            nodeStyle.border = new RectOffset(12, 12, 12, 12);
            nodeStyle.alignment = TextAnchor.MiddleCenter;

            this.id = id;

            this.name = name;

            if (newConversation)
            {
                Node rootNode = new(new Vector2((parent.position.width - 175) / 2, 150), 200, 50, nodeStyle, this, false);
                nodes.Add(rootNode);
                parent.allNodes.Add(rootNode);

                this.rootNode = rootNode;
            }

            DrawNodes();

            contentRect = new((parent.position.width - 300) / 2, 100, 300, 300);
        }

        public void DrawNodes()
        {
            if (nodes != null)
            {
                foreach (Node node in nodes)
                {
                    node.Draw();
                }
            }
        }

        public Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        public void ProcessEvents(Event e)
        {
            foreach (Node node in nodes)
            {
                switch (e.type)
                {
                    case EventType.MouseDown:
                        if (e.button == 0 && node.renaming && !node.rect.Contains(e.mousePosition))
                        {
                            node.Rename();
                        }

                        if (!node.showingContent) {
                            if (e.button == 0 && !node.rect.Contains(e.mousePosition))
                            {
                                bool canDeselect = true;
                                foreach (Node node1 in nodes)
                                {
                                    if (node1.isCreating)
                                    {
                                        canDeselect = false;
                                        break;
                                    }
                                }

                                if (!node.selected)
                                    canDeselect = false;

                                if (canDeselect)
                                {
                                    node.Deselect();
                                    node.style.normal.background = nodeBackground;
                                    GUI.FocusControl(null);
                                    if (node.showingContent)
                                        node.ShowContent();
                                }
                            }
                        } else {
                            if (e.button == 0 && !contentRect.Contains(e.mousePosition)) 
                            {
                                bool canDeselect = true;
                                foreach (Node node1 in nodes)
                                {
                                    if (node1.isCreating)
                                    {
                                        canDeselect = false;
                                        break;
                                    }
                                }

                                if (!node.selected)
                                    canDeselect = false;

                                if (canDeselect)
                                {
                                    node.Deselect();
                                    node.style.normal.background = nodeBackground;
                                    GUI.FocusControl(null);
                                    if (node.showingContent)
                                        node.ShowContent();
                                }
                            }
                        }

                        if (e.button == 1 && node.rect.Contains(e.mousePosition))
                        {
                            ProcessContextMenu(node);
                        }
                        else if (e.button == 0 && node.rect.Contains(e.mousePosition))
                        {
                            if (node.isCreating)
                            {
                                node.isCreating = false;
                                node.style.normal.background = nodeBackground;
                            }
                        }
                        break;

                    case EventType.KeyDown:
                        if (e.keyCode == KeyCode.Return && node.selected && !node.showingContent && !parentIsaddingCharacter)
                        {
                            node.Rename();
                        }

                        if (e.keyCode == KeyCode.E && node.selected)
                        {
                            if (!node.showingContent && !parentIsaddingCharacter)
                                node.ShowContent();
                        }
                        break;

                    case EventType.MouseDrag:
                        canDrag = true;

                        foreach (Node i in nodes)
                        {
                            if (i.isDragged) {
                                canDrag = false;
                            }
                        }

                        if (e.button == 0 && canDrag)
                        {
                            OnDrag(e.delta * 0.75f);
                        }
                        break;
                }
            }

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        mouseOverNodes.Clear();
                        nodeToSelect = null;
                        otherNodeIsSelected = false;

                        foreach (Node node in nodes)
                        {
                            if (node.selected)
                            {
                                otherNodeIsSelected = true;
                            }
                        }

                        if (!otherNodeIsSelected)
                        {
                            foreach (Node i in nodes)
                            {
                                if (i.rect.Contains(e.mousePosition))
                                {
                                    mouseOverNodes.Add(i);
                                }
                            }

                            if (mouseOverNodes.Count > 1)
                            {
                                foreach (Node i in mouseOverNodes)
                                {
                                    if (nodeToSelect != null)
                                    {
                                        if (i.rect.position.y < nodeToSelect.rect.position.y)
                                        {
                                            nodeToSelect = i;
                                        }
                                    }
                                    else
                                    {
                                        nodeToSelect = i;
                                    }
                                }
                            }
                            else if (mouseOverNodes.Count != 0)
                            {
                                nodeToSelect = mouseOverNodes[0];
                            }


                            if (nodeToSelect != null)
                            {
                                nodeToSelect.style.normal.background = nodeBackgroundSelected;
                            }
                            nodeToSelect?.Select();
                        }
                    }
                    break;
            }
        }

        void OnDrag(Vector2 delta)
        {
            if (nodes != null)
            {
                foreach (Node node in nodes)
                {
                    node.Drag(delta);
                }
            }
        }

        public void ProcessNodeEvents(Event e)
        {
            if (nodes != null)
            {
                for (int i = nodes.Count - 1; i >= 0; i--)
                {
                    bool guiChanged = nodes[i].ProcessEvents(e);

                    if (guiChanged)
                    {
                        GUI.changed = true;
                    }
                }
            }
        }

        private void ProcessContextMenu(Node node)
        {
            GenericMenu genericMenu = new GenericMenu();
            genericMenu.AddItem(new GUIContent("Add child node"), false, node.OnClickAddNode);

            if (node.isRoot)
            {
                genericMenu.AddItem(new GUIContent("Delete node"), false, DeleteNode, node);
            } else
            {
                genericMenu.AddDisabledItem(new GUIContent("Delete node"), false);
            }

            genericMenu.ShowAsContext();
        }

        private void DeleteNode(object obj)
        {
            Node node = (Node)obj;
            List<Node> children = new();

            node.parentNode.DeleteChild(node);

            foreach(Node child in node.children)
            {
                children.Add(child);
            }
            foreach (Node child in children)
            {
                DeleteNode(child);
            }

            nodes.Remove(node);
        }
    }
}

public class Node
{
    public Rect rect;
    public string title = "New Node";

    public GUIStyle style;

    public bool selected;
    public bool renaming;

    public Node parentNode;

    public bool isDragged;
    public bool isCreating;
    public bool showingContent;

    public List<Node> children = new();
    DialogueManager.Conversation parentConversation;
    public string content = "";

    public bool isRoot;

    public string character;

    public Node(Vector2 position, float width, float height, GUIStyle nodeStyle, DialogueManager.Conversation parentConversation, bool wasSaved, Node parentNode = null)
    {
        if (parentNode != null)
        {
            this.parentNode = parentNode;
            rect = new Rect(parentNode.rect.position.x, parentNode.rect.position.y + 100, width, height);
            isRoot = false;

            if (!wasSaved) isCreating = true;
        }
        else
        {
            rect = new Rect(position.x, position.y, width, height);
            isRoot = true;
        }

        style = nodeStyle;
        this.parentConversation = parentConversation;
    }

    public void Drag(Vector2 delta)
    {
        if (!showingContent)
        {
            rect.position += delta;
        }
    }

    public void Draw()
    {
        GUI.Box(rect, title, style);
    }

    public bool ProcessEvents(Event e)
    {
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0)
                {
                    if (rect.Contains(e.mousePosition))
                    {
                        isDragged = true;
                        GUI.changed = true;
                    }
                    else
                    {
                        GUI.changed = true;
                    }
                }
                break;

            case EventType.MouseUp:
                isDragged = false;
                break;

            case EventType.MouseDrag:
                if (e.button == 0 && isDragged)
                {
                    Drag(e.delta);
                    e.Use();
                    return true;
                }
                break;
        }
        return false;
    }

    public void OnClickAddNode()
    {
        GUIStyle childStyle = new GUIStyle();
        childStyle.normal.background = parentConversation.nodeBackground;
        childStyle.normal.textColor = parentConversation.lightShades;
        childStyle.border = new RectOffset(12, 12, 12, 12);
        childStyle.alignment = TextAnchor.MiddleCenter;
        Node childNode = new Node(new Vector2(0, 0), 200, 50, childStyle, parentConversation, false, this);
        children.Add(childNode);
        parentConversation.nodes.Add(childNode);
        parentConversation.parent.allNodes.Add(childNode);
    }

    public void Select()
    {
        selected = true;
    }

    public void Deselect()
    {
        selected = false;
    }

    public void Rename()
    {
        renaming = !renaming;
    }

    public void ShowContent()
    {
        showingContent = !showingContent;
    }

    public void DeleteChild(Node child)
    {
        children.Remove(child);
    }

    public string PosToSave()
    {
        return "(" + rect.position.x + "\\endX" + rect.position.y + ")";
    }

    public List<object> ListToSave()
    {
        List<object> e = new();

        e.Add(title);
        e.Add(content);
        e.Add(PosToSave());

        if (children.Count > 0)
        {
            List<List<object>> childrenList = new();
            foreach (Node child in children)
            {
                childrenList.Add(child.ListToSave());
            }
            e.Add(childrenList);
        }

        return e;
    }
}