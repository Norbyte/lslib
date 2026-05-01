#include "PhysicsTool.h"
#include <unordered_map>
#include <string>

#define PR(name, type, def) LoadProperty<type>(ele, #name, def)
#define P(name, type) LoadProperty<type>(ele, #name)
#define SET_PR(name, type, def) o->set##name(LoadProperty<type>(ele, #name, def))
#define SET_P(name, type) o->set##name(LoadProperty<type>(ele, #name))
#define SET_FLAG(name, prop, enumlbl) o->set##prop(enumlbl, LoadProperty<bool>(ele, #name, false))
#define SET_PB(name, bound) o->set##name(LoadBoundedProperty(ele, #name, bound))

using namespace physx;

class PhysXLoader {
public:
    PxPhysics* physics_{ nullptr };
    PxCooking* cooking_{ nullptr };
    PxCollection* collection_{ nullptr };
    std::unordered_map<uint32_t, PxMaterial*> materials_;
    std::unordered_map<std::string, PxRigidActor*> actors_;

    PxCollection* Load(TiXmlElement& doc);
    void LoadTopLevel(TiXmlElement& ele);
    PxBase* LoadMaterial(TiXmlElement& ele);
    PxBase* LoadRigidStatic(TiXmlElement& ele);
    PxBase* LoadRigidDynamic(TiXmlElement& ele);
    void LoadRigidActor(TiXmlElement& ele, PxRigidActor* o);
    void LoadRigidBody(TiXmlElement& ele, PxRigidBody* o);
    PxShape* LoadShape(TiXmlElement& ele);
    PxGeometry* LoadGeometry(TiXmlElement& ele);
    PxReal LoadBoundedProperty(TiXmlElement& ele, char const* name, PxReal bound);

    template <class T> T LoadProperty(TiXmlElement& ele, char const* name, T defaultVal);
    template <class T> T LoadProperty(TiXmlElement& ele, char const* name);
};

template <> PxReal PhysXLoader::LoadProperty<PxReal>(TiXmlElement& ele, char const* name, PxReal def) {
    auto attr = ele.FirstChildElement(name);
    return attr ? std::stof(attr->GetText()) : def;
}

template <>
std::string PhysXLoader::LoadProperty<std::string>(TiXmlElement& ele, char const* name, std::string defaultVal) {
    auto attr = ele.FirstChildElement(name);
    if (!attr || !attr->GetText()) return defaultVal;
    return std::string(attr->GetText());
}

template <>
unsigned int PhysXLoader::LoadProperty<unsigned int>(TiXmlElement& ele, char const* name, unsigned int defaultVal) {
    auto attr = ele.FirstChildElement(name);
    if (!attr || !attr->GetText()) return defaultVal;
    return (unsigned int)std::stoul(attr->GetText());
}

template <class T>
T PhysXLoader::LoadProperty(TiXmlElement& ele, char const* name, T defaultVal) {
    return defaultVal;
}

template <> PxQuat PhysXLoader::LoadProperty<PxQuat>(TiXmlElement& ele, char const* name) {
    auto attr = ele.FirstChildElement(name);
    if (!attr) return PxQuat(PxIdentity);
    return PxQuat(LoadProperty<PxReal>(*attr, "X", 0.0f), LoadProperty<PxReal>(*attr, "Y", 0.0f),
        LoadProperty<PxReal>(*attr, "Z", 0.0f), LoadProperty<PxReal>(*attr, "W", 1.0f));
}

template <> PxVec3 PhysXLoader::LoadProperty<PxVec3>(TiXmlElement& ele, char const* name) {
    auto attr = ele.FirstChildElement(name);
    if (!attr) return PxVec3(0);
    return PxVec3(LoadProperty<PxReal>(*attr, "X", 0.0f), LoadProperty<PxReal>(*attr, "Y", 0.0f), LoadProperty<PxReal>(*attr, "Z", 0.0f));
}

template <> PxTransform PhysXLoader::LoadProperty<PxTransform>(TiXmlElement& ele, char const* name) {
    auto attr = ele.FirstChildElement(name);
    if (!attr) return PxTransform(PxIdentity);
    return PxTransform(LoadProperty<PxVec3>(*attr, "Position"), LoadProperty<PxQuat>(*attr, "Rotation"));
}

PxCollection* PhysXLoader::Load(TiXmlElement& doc) {
    collection_ = PxCreateCollection();
    for (auto child = doc.FirstChildElement(); child; child = child->NextSiblingElement()) LoadTopLevel(*child);
    return collection_;
}

void PhysXLoader::LoadTopLevel(TiXmlElement& ele) {
    std::string type = ele.Value();
    if (type == "Material") LoadMaterial(ele);
    else if (type == "RigidStatic") LoadRigidStatic(ele);
    else if (type == "RigidDynamic") LoadRigidDynamic(ele);
}

PxBase* PhysXLoader::LoadMaterial(TiXmlElement& ele) {
    auto mat = physics_->createMaterial(PR(StaticFriction, PxReal, 1.0f), PR(DynamicFriction, PxReal, 1.0f), PR(Restitution, PxReal, 0.0f));
    materials_[PR(Index, PxU32, 0)] = mat;
    collection_->add(*mat);
    return mat;
}

PxBase* PhysXLoader::LoadRigidStatic(TiXmlElement& ele) {
    auto o = physics_->createRigidStatic(P(GlobalPose, PxTransform));
    LoadRigidActor(ele, o);
    collection_->add(*o);
    return o;
}

PxBase* PhysXLoader::LoadRigidDynamic(TiXmlElement& ele) {
    auto o = physics_->createRigidDynamic(P(GlobalPose, PxTransform));
    LoadRigidBody(ele, o);
    collection_->add(*o);
    return o;
}

void PhysXLoader::LoadRigidActor(TiXmlElement& ele, PxRigidActor* o) {
    o->setName(_strdup(PR(Name, std::string, "").c_str()));

    auto shapesEle = ele.FirstChildElement("Shapes");
    if (shapesEle) {
        for (auto sEle = shapesEle->FirstChildElement("Shape"); sEle; sEle = sEle->NextSiblingElement("Shape")) {
            PxShape* shape = LoadShape(*sEle);
            if (shape) o->attachShape(*shape);
        }
    }

    actors_[o->getName()] = o;
}

void PhysXLoader::LoadRigidBody(TiXmlElement& ele, PxRigidBody* o) {
    LoadRigidActor(ele, o);

    SET_PR(CMassLocalPose, PxTransform, PxTransform(PxIdentity));
    SET_PR(Mass, PxReal, 1.0f);
    SET_PR(MassSpaceInertiaTensor, PxVec3, PxVec3(1.0f, 1.0f, 1.0f));
    SET_PR(LinearDamping, PxReal, 0.0f);
    SET_PR(AngularDamping, PxReal, 0.05f);

    SET_PB(MaxLinearVelocity, 1e+15f);
    SET_PR(MaxAngularVelocity, PxReal, 100.0f);

    SET_FLAG(Kinematic, RigidBodyFlag, PxRigidBodyFlag::eKINEMATIC);
    SET_FLAG(EnableCCD, RigidBodyFlag, PxRigidBodyFlag::eENABLE_CCD);
    SET_FLAG(EnableCCDFriction, RigidBodyFlag, PxRigidBodyFlag::eENABLE_CCD_FRICTION);
    SET_FLAG(EnableSpeculativeCCD, RigidBodyFlag, PxRigidBodyFlag::eENABLE_SPECULATIVE_CCD);
    SET_FLAG(EnableCCDMaxContactImpulse, RigidBodyFlag, PxRigidBodyFlag::eENABLE_CCD_MAX_CONTACT_IMPULSE);
    SET_FLAG(RetainAccelerations, RigidBodyFlag, PxRigidBodyFlag::eRETAIN_ACCELERATIONS);

    SET_PR(MinCCDAdvanceCoefficient, PxReal, 0.15f);
    SET_PB(MaxDepenetrationVelocity, 1e+31f);
    SET_PB(MaxContactImpulse, 1e+31f);
}


PxReal PhysXLoader::LoadBoundedProperty(TiXmlElement& ele, char const* name, PxReal bound) {
    auto attr = ele.FirstChildElement(name);
    if (!attr || !attr->GetText()) return bound;
    if (strcmp(attr->GetText(), "Unbounded") == 0) return bound;
    return std::stof(attr->GetText());
}

PxShape* PhysXLoader::LoadShape(TiXmlElement& ele) {
    auto matIdx = PR(MaterialIndex, unsigned int, 0);
    auto mat = materials_[matIdx];
    if (!mat) throw std::runtime_error("Shape references undefined material index");

    auto geomEle = ele.FirstChildElement("Geometry");
    if (!geomEle) return nullptr;

    PxGeometry* geom = LoadGeometry(*geomEle);
    if (!geom) return nullptr;

    PxShape* o = physics_->createShape(*geom, *mat, true);
    o->setName(_strdup(PR(Name, std::string, "").c_str()));
    o->setLocalPose(P(LocalPose, PxTransform));

    delete geom;
    return o;
}

PxGeometry* PhysXLoader::LoadGeometry(TiXmlElement& ele) {
    std::string type = PR(Type, std::string, "");
    if (type == "Box") return new PxBoxGeometry(P(HalfExtents, PxVec3));
    if (type == "Sphere") return new PxSphereGeometry(PR(Radius, PxReal, 1.0f));
    return nullptr;
}

PxCollection* PhysXConverter::LoadCollectionFromXml(std::span<uint8_t> const& xml) {
    PhysXLoader loader;
    loader.physics_ = physics_;
    loader.cooking_ = cooking_;
    std::string s((char const*)xml.data(), xml.size());
    TiXmlDocument doc;
    doc.Parse(s.c_str());
    return loader.Load(*doc.FirstChildElement());
}
