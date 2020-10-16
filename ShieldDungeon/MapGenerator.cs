using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Room))]
public class MapGenerator : MonoBehaviour
{
    #region Editor Fields
    [Header("Mapgenerator general variables")]
    [SerializeField] private int _width;
    [SerializeField] private int _height;
    [SerializeField] private string _seed;
    [SerializeField] private bool _useRandomSeed;
    [SerializeField, Range(0,100)] private int _randomFillPercent;

    [Header("Mapgenerator tweaking variables")]
    //Dictates how much smoothing is needed, high smoothingTreshhold == more empty space
    [SerializeField, Range(0,20)] private int _amountOfSmoothingPhases = 4;
    [SerializeField, Range(3, 5)] private int _smoothingTreshhold = 4;
    //Dictates the amount of smoothing needed for rooms
    [SerializeField, Range(0, 20)] private int _amountOfRoomSmoothingPhases = 4;
    [SerializeField, Range(3, 5)] private int _roomSmoothingTreshhold = 4;
    //Delete room and wall regions under this treshhold
    [SerializeField] private int _wallTreshholdSize = 5;
    [SerializeField] private int _roomTreshholdSize = 50;
    //Sets size of the corridors that connect the rooms
    [SerializeField, Range(1,20)] private int _passageSize = 2;
    //Adds a border to the map
    [SerializeField, Range(1,20)] private int _borderSize = 3;

    [Header("Gameplay elements")]
    [SerializeField] private GameObject _player;
    [SerializeField] private GameObject _shieldSpawner;
    [SerializeField] private List<GameObject> _enemies;
    [SerializeField] private Camera _camera;
    [SerializeField] private GameObject _door;
    #endregion

    #region Fields
    //List of all the coordinates in the map
    private int[,] map;
    //Holds a list of all the rooms in the map
    private List<Room> _allRooms = new List<Room>();
    #endregion

    #region Functions
    private void Awake()
    {
        //Only generate the map if the player is actually valid
        if (_player != null && _camera != null)
        {
            GenerateMap();
        }
        else
        {
            Debug.LogError("Map could not be generated, Player or Camera is null");
        }
    }
    private void Update()
    {
        //GenerateNewMap();

    }

    #region MapGeneration
    //Generates a new map on mouseclick, used in debug to test random map generation
    private void GenerateNewMap()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (_allRooms != null)
            {
                _allRooms.Clear();
            }
            GenerateMap();
        }
    }

    //Generate the entire map
    private void GenerateMap()
    {
        map = new int[_width, _height];
        RandomFillMap();

        for (int i = 0; i < _amountOfSmoothingPhases; i++)
        {
            SmoothMap();
        }

        ProcessMap();

        int[,] borderedMap = new int[_width + _borderSize * 2, _height + _borderSize * 2];
        for (int x = 0; x < borderedMap.GetLength(0); x++)
        {
            for (int y = 0; y < borderedMap.GetLength(1); y++)
            {
                if(x >= _borderSize && x < _width + _borderSize && y >= _borderSize && y < _height + _borderSize)
                {
                    borderedMap[x, y] = map[x - _borderSize, y - _borderSize];
                }
                else
                {
                    borderedMap[x, y] = 1;
                }
            }
        }

        //Create the actual mesh of the level
        MeshGenerator meshGen = GetComponent<MeshGenerator>();
        meshGen.GenerateMesh(borderedMap, 1);
        //Add player, enemies and other gameobjects
        AddGameplayElements();
    }

    //Randomly fill up the map with wall and empty tiles
    void RandomFillMap()
    {
        if (_useRandomSeed)
        {
            _seed = Time.time.ToString();
        }

        System.Random randNumber = new System.Random(_seed.GetHashCode());

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                //Add a wall border around the entire map to make sure the player can't walk out of the map
                if (x == 0 || x == _width - 1 || y == 0 || y == _height - 1)
                {
                    map[x, y] = 1;
                }
                else
                {
                    map[x, y] = (randNumber.Next(0, 100) < _randomFillPercent) ? 1 : 0;
                }
            }
        }
    }

    #endregion

    #region Smoothing
    //Smooth map by deleting single wall and empty tiles
    void SmoothMap()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                //Get the amount of wall tiles around the current location and 
                //turn this into a wall tile if there are more wall tiles than the treshhold around the tile
                ///TODO: Add a  second list to set the new tiles in, then switch to use that map. Current method creates a biased map
                int neighbourWallTiles = GetSurroundingWallCount(x, y);
                if (neighbourWallTiles > _smoothingTreshhold)
                {
                    map[x, y] = 1;
                }
                else if (neighbourWallTiles < _smoothingTreshhold)
                {
                    map[x, y] = 0;
                }
            }
        }
    }
    void SmoothRooms(List<List<Coord>> rooms)
    {
        for (int i = 0; i < _amountOfRoomSmoothingPhases; i++)
        {
            foreach (List<Coord> room in rooms)
            {
                foreach (Coord coord in room)
                {
                    int neighbourWallTiles = GetSurroundingWallCount(coord.tileX, coord.tileY);
                    if (neighbourWallTiles > _roomSmoothingTreshhold)
                    {
                        map[coord.tileX, coord.tileY] = 1;
                    }
                    else if (neighbourWallTiles <= _roomSmoothingTreshhold)
                    {
                        map[coord.tileX, coord.tileY] = 0;
                    }
                }
            }
        }
    }

    //Delete smaller rooms and wall pieces
    void ProcessMap()
    {
        List<List<Coord>> wallRegions = GetRegions(1);

        foreach(List<Coord> wallRegion in wallRegions)
        {
            if(wallRegion.Count < _wallTreshholdSize)
            {
                foreach(Coord tile in wallRegion)
                {
                    map[tile.tileX, tile.tileY] = 0;
                }
            }
        }

        List<List<Coord>> roomRegions = GetRegions(0);
        SmoothRooms(roomRegions);
        roomRegions = GetRegions(0);
        foreach (List<Coord> roomRegion in roomRegions)
        {
            if (roomRegion.Count < _roomTreshholdSize)
            {
                foreach (Coord tile in roomRegion)
                {
                    map[tile.tileX, tile.tileY] = 1;
                }
            }
            else
            {
                _allRooms.Add(new Room(roomRegion, map));
            }
        }
        _allRooms.Sort();
        _allRooms[0].SetMainRoom();

        ConnectClosestRooms(_allRooms);
    }
    #endregion
    
    #region Floodfill algorithm
    //Divide map into map and room regions using the floodfill algorithm
    List<List<Coord>> GetRegions(int tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] mapFlags = new int[_width, _height];
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                {
                    List<Coord> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);
                    foreach (Coord tile in newRegion)
                    {
                        mapFlags[tile.tileX, tile.tileY] = 1;
                    }
                }
            }
        }
        return regions;
    }

    //FloodFill algorithm
    List<Coord> GetRegionTiles(int startX, int startY)
    {
        List<Coord> tiles = new List<Coord>();
        int[,] mapFlags = new int[_width, _height];
        int tileType = map[startX, startY];

        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY));

        mapFlags[startX, startY] = 1;

        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
            {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                {
                    if (IsInMapRange(x, y) && (y == tile.tileY || x == tile.tileX))
                    {
                        if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                        {
                            mapFlags[x, y] = 1;
                            queue.Enqueue(new Coord(x, y));
                        }
                    }
                }
            }
        }
        return tiles;
    }
    #endregion

    #region RoomConnections
    //Connect all surviving rooms in the map with eachother
    void ConnectClosestRooms(List<Room> allRooms, bool forceAccessibilityFromMainRoom = false)
    {
        List<Room> roomListA = new List<Room>();
        List<Room> roomListB = new List<Room>();

        //Final pass over all rooms to make sure they are all connected to the main room
        if(forceAccessibilityFromMainRoom)
        {
            foreach(Room room in allRooms)
            {
                if(room.IsAccessibleFromMainRoom)
                {
                    roomListB.Add(room);
                }
                else
                {
                    roomListA.Add(room);
                }
            }
        }
        else
        {
            roomListA = allRooms;
            roomListB = allRooms;
        }

        int bestDistance = 0;
        Coord bestTileA = new Coord();
        Coord bestTileB = new Coord();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;

        foreach (Room roomA in roomListA)
        {
            if(!forceAccessibilityFromMainRoom)
            {
                //First pass, make sure all rooms are at least connected to 1 other room
                possibleConnectionFound = false;
                if(roomA.ConnectedRooms.Count > 0)
                {
                    continue;
                }
            }
            foreach(Room roomB in roomListB)
            {
                if(roomA == roomB || roomA.IsConnected(roomB))
                {
                    continue;
                }
                for(int tileIndexA = 0; tileIndexA < roomA.EdgeTiles.Count; tileIndexA++)
                {
                    for (int tileIndexB = 0; tileIndexB < roomB.EdgeTiles.Count; tileIndexB++)
                    {
                        //Check the distance between all the tiles of both rooms to find the 2 tiles closest to eachother
                        Coord tileA = roomA.EdgeTiles[tileIndexA];
                        Coord tileB = roomB.EdgeTiles[tileIndexB];
                        int distanceBetweenRooms = (int)(Mathf.Pow(tileA.tileX - tileB.tileX, 2) + Mathf.Pow(tileA.tileY - tileB.tileY, 2));

                        if(distanceBetweenRooms < bestDistance || !possibleConnectionFound)
                        {
                            bestDistance = distanceBetweenRooms;
                            possibleConnectionFound = true;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }
            }
            if(possibleConnectionFound && !forceAccessibilityFromMainRoom) //Connect the rooms and move on to the next room
            {
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            }
        }

        if(possibleConnectionFound &&  forceAccessibilityFromMainRoom)
        {
            CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            ConnectClosestRooms(allRooms, true);
        }

        if (!forceAccessibilityFromMainRoom)
        {
            ConnectClosestRooms(allRooms, true);
        }
    }
    //Flip the wall tiles in the map to empty tiles to connect the rooms in the map
    void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB)
    {
        //Opted for a straight connection instead of a diagonal one, use GetLine for a diagonal connection
        Room.ConnectRooms(roomA, roomB);
        List<int> lineX = GetStraightLine(tileA.tileX, tileB.tileX);
        List<int> lineY = GetStraightLine(tileA.tileY, tileB.tileY);
        if (UnityEngine.Random.value <= 0.5f)
        {
            foreach(int x in lineX)
            {
                DrawCircle(new Coord(x, tileA.tileY), _passageSize);
            }
            foreach(int y in lineY)
            {
                DrawCircle(new Coord(tileB.tileX, y), _passageSize);
            }
        }
        else
        {
            foreach (int x in lineX)
            {
                DrawCircle(new Coord(x, tileB.tileY), _passageSize);
            }
            foreach (int y in lineY)
            {
                DrawCircle(new Coord(tileA.tileX, y), _passageSize);
            }
        }
    }
    List<Coord> GetLine(Coord startPoint, Coord endPoint)
    {
        List<Coord> line = new List<Coord>();

        int x = startPoint.tileX;
        int y = startPoint.tileY;

        int dx = endPoint.tileX - startPoint.tileX;
        int dy = endPoint.tileY - startPoint.tileY;

        bool inverted = false;
        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);

        int longest = Mathf.Abs(dx);
        int shortest = Mathf.Abs(dy);

        if (longest < shortest)
        {
            inverted = true;
            longest = Mathf.Abs(dy);
            shortest = Mathf.Abs(dx);

            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }

        int gradientAccumulation = longest / 2;
        for (int i = 0; i < longest; i++)
        {
            line.Add(new Coord(x, y));
            if (inverted)
            {
                y += step;
            }
            else
            {
                x += step;
            }

            gradientAccumulation += shortest;
            if (gradientAccumulation >= longest)
            {
                if (inverted)
                {
                    x += gradientStep;
                }
                else
                {
                    y += gradientStep;
                }
                gradientAccumulation -= longest;
            }
        }
        return line;
    }
    List<int> GetStraightLine(int start, int end)
    {
        List<int> line = new List<int>();

        int length = end - start;
        float step = Mathf.Sign(length);
        length = Mathf.Abs(length);

        for (int i = 0; i < length; i++)
        {
            line.Add(start + (int)step * i);
        }
        return line;
    }
    void DrawCircle(Coord c, int r)
    {
        for(int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                if(x*x + y*y <= r*r)
                {
                    int drawX = c.tileX + x;
                    int drawY = c.tileY + y;
                    if(IsInMapRange(drawX, drawY))
                    {
                        map[drawX, drawY] = 0;
                    }
                }
            }
        }
    }
    #endregion

    #region Return Functions
    Vector3 CoordToWorldPoint(Coord tile)
    {
        //Converts the local coordinate to unity world position
        return new Vector3(-_width / 2 + 0.5f + tile.tileX, 2, -_height / 2 + 0.5f + tile.tileY);
    }
    //Check if the tile in question is actually in the map
    bool IsInMapRange(int x, int y)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height;
    }
    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
        { 
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
            {
                if (IsInMapRange(neighbourX, neighbourY))
                {
                    if (neighbourX != gridX || neighbourY != gridY)
                    {
                        wallCount += map[neighbourX, neighbourY];
                    }
                }
                else
                {
                    wallCount++;
                }
            }
        }
        return wallCount;
    }
    #endregion

    #region GamePlay
    void AddGameplayElements()
    {
        const int randChestroomPercentage = 20;

        _allRooms[0].RoomType = Room.Type.Start;
        for(int i = 1; i < _allRooms.Count; i++)
        {
            if(randChestroomPercentage >= UnityEngine.Random.Range(0, 100))
            {
                _allRooms[i].RoomType = Room.Type.Chest;
            }
            else
            {
                _allRooms[i].RoomType = Room.Type.Enemy;
            }
        }
        SpawnRoomElements();
    }

    private void SpawnRoomElements()
    {
        int amountOfEnemiesPerRoom = 5;
        List<Coord> randomTilesInRoom = new List<Coord>();

        foreach (Room room in _allRooms)
        {
            switch (room.RoomType)
            {
                //Starting room needs player and shield
                case Room.Type.Start:
                    Coord randomTileInRoom = room.GetRandomTile();
                    Vector3 playerPosInRoom = CoordToWorldPoint(randomTileInRoom);
                    Vector3 shieldPosInRoom = CoordToWorldPoint(room.GetCoordInRange(randomTileInRoom, 2));
                    Instantiate(_player, playerPosInRoom, Quaternion.identity);
                    Instantiate(_shieldSpawner, shieldPosInRoom, Quaternion.identity);
                    Instantiate(_camera);
                    break;
                //Chest room needs shield spawn
                case Room.Type.Chest:
                    Vector3 randPosInRoom = CoordToWorldPoint(room.GetRandomTile());
                    Instantiate(_shieldSpawner, randPosInRoom, Quaternion.identity);
                    break;
                //Enemy room needs an amount of enemies
                case Room.Type.Enemy:
                    randomTilesInRoom = room.GetRandomTiles(amountOfEnemiesPerRoom);
                    foreach (Coord randTile in randomTilesInRoom)
                    {
                        Vector3 randEnemyPos = CoordToWorldPoint(randTile);
                        SpawnRandomEnemy(randEnemyPos);
                    }
                    break;
                default:
                    break;
            }
        }
    }

    void SpawnRandomEnemy(Vector3 randPos)
    {
        Instantiate(_enemies[UnityEngine.Random.Range(0, _enemies.Count)], randPos, Quaternion.identity);
    }

    #endregion
    #endregion  
}
