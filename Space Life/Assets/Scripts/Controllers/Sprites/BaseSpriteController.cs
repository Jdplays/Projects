using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseSpriteController<T> 
{
    protected Dictionary<T, GameObject> objectGameObjectMap;
    protected World world;
    protected GameObject objectParent;

    public BaseSpriteController(World world, string parentName)
    {
        this.world = world;
        objectParent = new GameObject(parentName);
        objectGameObjectMap = new Dictionary<T, GameObject>();
    }

    public virtual void RemoveAll()
    {
        objectGameObjectMap.Clear();
        GameObject.Destroy(objectParent);
    }

    protected abstract void OnCreated(T obj);

    protected abstract void OnChanged(T obj);

    protected abstract void OnRemoved(T obj);
}
