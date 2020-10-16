using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct Coord
{
    public int tileX;
    public int tileY;

    public Coord(int x, int y)
    {
        tileX = x;
        tileY = y;
    }
}

class Room : IComparable<Room>
{
    #region Fields
    private List<Coord> tiles;
    private int roomSize;
    private bool isMainRoom = false;
    #endregion

    #region Properties
    public List<Coord> EdgeTiles { get; private set; }
    public bool IsAccessibleFromMainRoom { get; private set; }
    public List<Room> ConnectedRooms {get; private set; }
    public Type RoomType { get; set; }
    #endregion

    #region Functions
    public enum Type
    {
        Enemy,
        Chest,
        Start
    }

    public Room()
    {

    }

    public Room(List<Coord> roomTiles, int[,] map)
    {
        RoomType = Type.Enemy;
        tiles = roomTiles;
        roomSize = tiles.Count;
        ConnectedRooms = new List<Room>();

        EdgeTiles = new List<Coord>();
        foreach (Coord tile in tiles)
        {
            for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
            {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                {
                    if (x == tile.tileX || y == tile.tileY)
                    {
                        if (map[x, y] == 1)
                        {
                            EdgeTiles.Add(tile);
                        }
                    }
                }
            }
        }
    }

    //Set this room and all rooms connected to this room as accesible from the main room
    private void SetAccessibleFromMainRoom()
    {
        if (!IsAccessibleFromMainRoom)
        {
            IsAccessibleFromMainRoom = true;
            foreach (Room connectedRoom in ConnectedRooms)
            {
                connectedRoom.SetAccessibleFromMainRoom();
            }
        }
    }

    public static void ConnectRooms(Room roomA, Room roomB)
    {
        if (roomA.IsAccessibleFromMainRoom)
        {
            roomB.SetAccessibleFromMainRoom();
        }
        else if (roomB.IsAccessibleFromMainRoom)
        {
            roomA.SetAccessibleFromMainRoom();
        }
        roomA.ConnectedRooms.Add(roomB);
        roomB.ConnectedRooms.Add(roomA);
    }

    public bool IsConnected(Room otherRoom)
    {
        return ConnectedRooms.Contains(otherRoom);
    }

    public int CompareTo(Room otherRoom)
    {
        return otherRoom.roomSize.CompareTo(roomSize);
    }

    //Get a random tile in the room
    public Coord GetRandomTile()
    {
        return tiles[UnityEngine.Random.Range(0, tiles.Count)];
    }

    //Get a list of different random tiles in the room
    public List<Coord> GetRandomTiles(int amount)
    {
        List<Coord> randomTiles = new List<Coord>();
        for (int i = 0; i < amount; i++)
        {
            Coord randTile = GetRandomTile();
            while (randomTiles.Contains(randTile))
            {
                randTile = GetRandomTile();
            }
            randomTiles.Add(randTile);
        }
        return randomTiles;
    }

    //Get a random coordinate around another coordinate in order to spawn items close to eachother
    public Coord GetCoordInRange(Coord aroundCoord, int range)
    {
        Coord coord = GetRandomTile();
        const int maxAmountOfTries = 2000;
        int amountOfTries = 0;
        while (Vector2.Distance(new Vector2(coord.tileX, coord.tileY), new Vector2(aroundCoord.tileX, aroundCoord.tileY)) > range)
        {
            coord = GetRandomTile();
            amountOfTries++;
            if (amountOfTries > maxAmountOfTries)
            {
                break;
            }
        }
        return coord;
    }

    public bool IsCoordInRoom(Coord coord)
    {
        if (tiles.Contains(coord))
        {
            return true;
        }
        return false;
    }

    public void SetMainRoom()
    {
        isMainRoom = true;
        IsAccessibleFromMainRoom = true;
    }
    #endregion
}
