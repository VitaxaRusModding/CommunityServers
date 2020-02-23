﻿using System;
using Community.Other;
using Community.Server.Components;
using LiteNetLib;
using LiteNetLib.Utils;
using Unity.Entities;
using UnityEngine;

namespace Community.Server.Systems
{
    public delegate void OnSpawnEntity(EntityNetManager manager);
    public class EntitysSystem : ComponentServer
    {
        EntitysManager entitysManager;
        public static OnSpawnEntity onSpawnEntity;
        protected override void onStartedServer(NetPacketProcessor _packetProcessor)
        {
            entitysManager = ServerManager.manager.GetManager<EntitysManager>(EManagers.entitysnet);
            CustomizeSystem.onCreateCharacter += onCreatePeer;
        }


        protected override void onConnectedPlayer(NetPeer peer)
        {
            CreatePlayer(peer);
        }  
        private void onCreatePeer(EntityNetManager entity)
        {
            EntityData data = LoadEntityData(entity.username);
            if (data != null)
                Spawn(entity, data.position, data.rot);
            else
            {
                Spawn(entity, entitysManager.SpawnEntity.position, 0);

            }
        }
        public virtual void Spawn(EntityNetManager entityPlayer, Vector3 position, float rot)
        {
            //  _buffer.FastClear();
            Quaternion rotation = Quaternion.Euler(0, rot, 0);
            GameObject pl = GameObject.Instantiate(Resources.Load("playerServer"), position, rotation) as GameObject;
            pl.name = "Entity_" + entityPlayer.id;
            entityPlayer.controller = pl.GetComponent<CharacterController>(); 
            entityPlayer.transform = pl.transform;  
            World.Active.EntityManager.AddComponentData(entityPlayer.entityWorld, new EntityMotor(position, rot));
            World.Active.EntityManager.AddComponentData(entityPlayer.entityWorld, new EntityRegion(0));
            World.Active.EntityManager.AddComponentData(entityPlayer.entityWorld, new EntityCitizen(0));
            World.Active.EntityManager.AddComponentData(entityPlayer.entityWorld, new EntityPosition(position));
            onSpawnEntity.Invoke(entityPlayer);

        }
        protected override void OnDisconectedPlayer(NetPeer peer, DisconnectInfo info)
        {
            EntityPlayerManager entityPlayer = (EntityPlayerManager)peer.Tag;
            if (entityPlayer != null)
            {
                SaveEntityData(entityPlayer.GetSave(),entityPlayer.username);
                entitysManager.Remove(entityPlayer);
                entityPlayer.DestroyEntity();
            }
            else Debug.LogError("[S] Player disconected, entity player not find");
        }
      
        private EntityPlayerManager CreatePlayer(NetPeer peer)
        {
            EntityPlayerManager entity = null;
            entitysManager.Add(entity);
            entity = new EntityPlayerManager( entitysManager.IndexOf(entity), EntityManager.CreateEntity(), peer);
            entitysManager.SetIndex(entity);
            entity.peer.Tag = entity;
            EntityManager.AddComponentData(entity.entityWorld, new EntityNetID(entity.id));
            EntityManager.AddComponentData(entity.entityWorld, new EntityNetPlayer()); 
            Debug.Log("[S] Entity create player " + entity.id);
           
            return entity;
        }

        private EntityNetManager CreateEntity()
        {
            EntityNetManager player = null;
            entitysManager.Add(player);
            player = new EntityNetManager((ushort)entitysManager.IndexOf(player), EntityManager.CreateEntity());
            EntityManager.AddComponentData(player.entityWorld, new EntityNetID(player.id));

            return player;

        }
        protected override void OnUpdate()
        {

        }
        private EntityData LoadEntityData(string username)
        {
           return  SaveManager.LoadJSON<EntityData>($"{ServerManager.manager.serverInfoProxy.serverFolder}/Players/player_{username}/player_{username}.dat");
        }
        private void SaveEntityData(EntityData data, string username)
        {
            SaveManager.CreateFolder($"{ServerManager.manager.serverInfoProxy.serverFolder}/Players/player_{username}/");
            SaveManager.SaveJSON(data, $"{ServerManager.manager.serverInfoProxy.serverFolder}/Players/player_{username}/player_{username}.dat");
        }
    }
    [System.Serializable]
    public class EntityData
    { 
        public Vector3 position;
        public float rot; 
        public EntityData(Vector3 vector3,float rotation)
        {
            position = vector3;
            rot = rotation;
        }
    }
}
