#pragma once

#define _USE_MATH_DEFINES

#include <vector>
#include <iostream>
#include <fstream>
#include <span>

#define WIN32_LEAN_AND_MEAN
#include <windows.h>


#include <PxPhysicsAPI.h>
#include <tinyxml.h>

using namespace physx;

class PhysXConverter
{
public:
    bool InitPhysX();
    void ShutdownPhysX();

    PxCollection* LoadCollectionFromXml(std::span<uint8_t> const& xml);
    PxCollection* LoadCollectionFromBinary(std::span<uint8_t> const& bin);

    std::vector<uint8_t> SaveCollectionToXml(PxCollection& collection);
    std::vector<uint8_t> SaveCollectionToBinary(PxCollection& collection);

private:
    PxFoundation* foundation_{ nullptr };
    PxPhysics* physics_{ nullptr };
    PxCooking* cooking_{ nullptr };
    PxSerializationRegistry* registry_{ nullptr };
};

