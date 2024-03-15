#include "PhysicsTool.h"
#include <unordered_map>

#define PR(name, type, def) LoadProperty<type>(ele, #name, def)
#define P(name, type) LoadProperty<type>(ele, #name)
#define PFLAG(name) LoadProperty<bool>(ele, #name, false)
#define P_BOUNDED(name, expr, bound) LoadBoundedProperty(ele, #name, bound)

#define SET_PR(name, type, def) o->set##name(LoadProperty<type>(ele, #name, def))
#define SET_P(name, type) o->set##name(LoadProperty<type>(ele, #name))
#define SET_FLAG(name, prop, enumlbl) o->set##prop(enumlbl, LoadProperty<bool>(ele, #name, false))
#define SET_PB(name, bound) o->set##name(LoadBoundedProperty(ele, #name, bound))

class PhysXLoader
{
public:
    PxPhysics* physics_;
    PxCooking* cooking_;

    PxCollection* collection_{ nullptr };

    std::unordered_map<uint32_t, PxMaterial*> materials_;
    std::unordered_map<std::string, PxRigidActor*> actors_;

    PxCollection* Load(TiXmlElement& doc)
    {
        collection_ = PxCreateCollection();

        for (auto child = doc.FirstChildElement(); child; child = child->NextSiblingElement()) {
            LoadTopLevel(*child);
        }

        return collection_;
    }

    template <class T>
    T LoadProperty(TiXmlElement& ele, char const* name, T defaultVal);

    PxReal LoadBoundedProperty(TiXmlElement& ele, char const* name, PxReal bound)
    {
        auto attr = ele.FirstChildElement(name);
        if (attr == nullptr) return bound;
        if (strcmp(attr->GetText(), "Unbounded") == 0) return bound;
        return std::stof(attr->GetText());
    }

    template <>
    PxReal LoadProperty(TiXmlElement& ele, char const* name, PxReal defaultVal)
    {
        auto attr = ele.FirstChildElement(name);
        if (attr == nullptr) return defaultVal;
        return std::stof(attr->GetText());
    }

    template <>
    PxU32 LoadProperty(TiXmlElement& ele, char const* name, PxU32 defaultVal)
    {
        auto attr = ele.FirstChildElement(name);
        if (attr == nullptr) return defaultVal;
        return (PxU32)std::stoi(attr->GetText());
    }

    template <>
    bool LoadProperty(TiXmlElement& ele, char const* name, bool defaultVal)
    {
        auto attr = ele.FirstChildElement(name);
        if (attr == nullptr) return defaultVal;
        return _stricmp(attr->GetText(), "true") == 0;
    }

    template <>
    std::string LoadProperty(TiXmlElement& ele, char const* name, std::string defaultVal)
    {
        auto attr = ele.FirstChildElement(name);
        if (attr == nullptr) return defaultVal;
        return attr->GetText();
    }

    template <class T>
    T LoadProperty(TiXmlElement& ele, char const* name);

    template <>
    std::string LoadProperty(TiXmlElement& ele, char const* name)
    {
        auto attr = ele.FirstChildElement(name);
        if (attr == nullptr) throw std::runtime_error(std::string("Missing property: ") + name);
        return attr->GetText();
    }

    template <>
    PxD6Motion::Enum LoadProperty(TiXmlElement& ele, char const* name)
    {
        auto attr = ele.FirstChildElement(name);
        if (attr == nullptr) throw std::runtime_error(std::string("Missing property: ") + name);
        
        if (strcmp(attr->GetText(), "Locked") == 0) return PxD6Motion::eLOCKED;
        if (strcmp(attr->GetText(), "Limited") == 0) return PxD6Motion::eLIMITED;
        return PxD6Motion::eFREE;
    }

    template <>
    PxTransform LoadProperty(TiXmlElement& ele, char const* name)
    {
        auto attr = ele.FirstChildElement(name);
        PxTransform tr;
        if (attr == nullptr) return tr;

        tr.p = LoadProperty<PxVec3>(*attr, "Position");
        tr.q = LoadProperty<PxQuat>(*attr, "Rotation");
        return tr;
    }

    template <>
    PxMeshScale LoadProperty(TiXmlElement& ele, char const* name)
    {
        auto attr = ele.FirstChildElement(name);
        PxMeshScale tr;
        if (attr == nullptr) return tr;

        tr.scale = LoadProperty<PxVec3>(*attr, "Scale");
        tr.rotation = LoadProperty<PxQuat>(*attr, "Rotation");
        return tr;
    }

    template <>
    PxVec3 LoadProperty(TiXmlElement& ele, char const* name)
    {
        auto attr = ele.FirstChildElement(name);
        PxVec3 v;
        if (attr == nullptr) return v;

        v.x = LoadProperty<PxReal>(*attr, "X", 0.0f);
        v.y = LoadProperty<PxReal>(*attr, "Y", 0.0f);
        v.z = LoadProperty<PxReal>(*attr, "Z", 0.0f);
        return v;
    }

    template <>
    PxVec3 LoadProperty(TiXmlElement& ele, char const* name, PxVec3 def)
    {
        auto attr = ele.FirstChildElement(name);
        if (attr == nullptr) return def;

        PxVec3 v;
        v.x = LoadProperty<PxReal>(*attr, "X", 0.0f);
        v.y = LoadProperty<PxReal>(*attr, "Y", 0.0f);
        v.z = LoadProperty<PxReal>(*attr, "Z", 0.0f);
        return v;
    }

    template <>
    PxQuat LoadProperty(TiXmlElement& ele, char const* name)
    {
        auto attr = ele.FirstChildElement(name);
        PxQuat v;
        if (attr == nullptr) return v;

        v.x = LoadProperty<PxReal>(*attr, "X", 0.0f);
        v.y = LoadProperty<PxReal>(*attr, "Y", 0.0f);
        v.z = LoadProperty<PxReal>(*attr, "Z", 0.0f);
        v.w = LoadProperty<PxReal>(*attr, "W", 0.0f);
        return v;
    }

    template <>
    PxJointLinearLimit LoadProperty(TiXmlElement& ele, char const* name)
    {
        auto attr = ele.FirstChildElement(name);
        PxJointLinearLimit v(PxTolerancesScale(), PX_MAX_F32);
        if (attr == nullptr) return v;

        v.value = LoadBoundedProperty(*attr, "Value", PX_MAX_F32);
        v.restitution = LoadProperty<PxReal>(*attr, "Restitution", 0.0f);
        v.bounceThreshold = LoadProperty<PxReal>(*attr, "BounceThreshold", 0.0f);
        v.stiffness = LoadProperty<PxReal>(*attr, "Stiffness", 0.0f);
        v.damping = LoadProperty<PxReal>(*attr, "Damping", 0.0f);
        v.contactDistance = LoadProperty<PxReal>(*attr, "ContactDistance", 0.0f);
        return v;
    }

    template <>
    PxJointLinearLimitPair LoadProperty(TiXmlElement& ele, char const* name)
    {
        auto attr = ele.FirstChildElement(name);
        PxJointLinearLimitPair v(PxTolerancesScale(), -PX_MAX_F32/3, PX_MAX_F32/3);
        if (attr == nullptr) return v;

        v.lower = LoadProperty<PxReal>(*attr, "Lower", -PX_MAX_F32/3);
        v.upper = LoadProperty<PxReal>(*attr, "Upper", PX_MAX_F32/3);
        v.restitution = LoadProperty<PxReal>(*attr, "Restitution", 0.0f);
        v.bounceThreshold = LoadProperty<PxReal>(*attr, "BounceThreshold", 0.0f);
        v.stiffness = LoadProperty<PxReal>(*attr, "Stiffness", 0.0f);
        v.damping = LoadProperty<PxReal>(*attr, "Damping", 0.0f);
        v.contactDistance = LoadProperty<PxReal>(*attr, "ContactDistance", 0.0f);
        return v;
    }

    template <>
    PxJointAngularLimitPair LoadProperty(TiXmlElement& ele, char const* name)
    {
        auto attr = ele.FirstChildElement(name);
        PxJointAngularLimitPair v(-PxPi / 2, PxPi / 2);
        if (attr == nullptr) return v;

        v.lower = LoadProperty<PxReal>(*attr, "Lower", -PxPi / 2);
        v.upper = LoadProperty<PxReal>(*attr, "Upper", PxPi / 2);
        v.restitution = LoadProperty<PxReal>(*attr, "Restitution", 0.0f);
        v.bounceThreshold = LoadProperty<PxReal>(*attr, "BounceThreshold", 0.0f);
        v.stiffness = LoadProperty<PxReal>(*attr, "Stiffness", 0.0f);
        v.damping = LoadProperty<PxReal>(*attr, "Damping", 0.0f);
        v.contactDistance = LoadProperty<PxReal>(*attr, "ContactDistance", 0.0f);
        return v;
    }

    template <>
    PxJointLimitCone LoadProperty(TiXmlElement& ele, char const* name)
    {
        auto attr = ele.FirstChildElement(name);
        PxJointLimitCone v(PxPi / 2, PxPi / 2);
        if (attr == nullptr) return v;

        v.yAngle = LoadProperty<PxReal>(*attr, "YAngle", PxPi / 2);
        v.zAngle = LoadProperty<PxReal>(*attr, "ZAngle", PxPi / 2);
        v.restitution = LoadProperty<PxReal>(*attr, "Restitution", 0.0f);
        v.bounceThreshold = LoadProperty<PxReal>(*attr, "BounceThreshold", 0.0f);
        v.stiffness = LoadProperty<PxReal>(*attr, "Stiffness", 0.0f);
        v.damping = LoadProperty<PxReal>(*attr, "Damping", 0.0f);
        v.contactDistance = LoadProperty<PxReal>(*attr, "ContactDistance", 0.0f);
        return v;
    }

    template <>
    PxJointLimitPyramid LoadProperty(TiXmlElement& ele, char const* name)
    {
        auto attr = ele.FirstChildElement(name);
        PxJointLimitPyramid v(-PxPi / 2, PxPi / 2, -PxPi / 2, PxPi / 2);
        if (attr == nullptr) return v;

        v.yAngleMin = LoadProperty<PxReal>(*attr, "YAngleMin", -PxPi / 2);
        v.yAngleMax = LoadProperty<PxReal>(*attr, "YAngleMax", PxPi / 2);
        v.zAngleMin = LoadProperty<PxReal>(*attr, "ZAngleMin", -PxPi / 2);
        v.zAngleMax = LoadProperty<PxReal>(*attr, "ZAngleMax", PxPi / 2);
        v.restitution = LoadProperty<PxReal>(*attr, "Restitution", 0.0f);
        v.bounceThreshold = LoadProperty<PxReal>(*attr, "BounceThreshold", 0.0f);
        v.stiffness = LoadProperty<PxReal>(*attr, "Stiffness", 0.0f);
        v.damping = LoadProperty<PxReal>(*attr, "Damping", 0.0f);
        v.contactDistance = LoadProperty<PxReal>(*attr, "ContactDistance", 0.0f);
        return v;
    }

    template <>
    PxD6JointDrive LoadProperty(TiXmlElement& ele, char const* name)
    {
        auto attr = ele.FirstChildElement(name);
        PxD6JointDrive v;
        if (attr == nullptr) return v;

        v.forceLimit = LoadBoundedProperty(*attr, "ForceLimit", PX_MAX_F32);
        v.flags = LoadProperty<bool>(*attr, "IsAcceleration", false) ? PxD6JointDriveFlag::eACCELERATION : (PxD6JointDriveFlag::Enum)0;
        v.stiffness = LoadProperty<PxReal>(*attr, "Stiffness", 0.0f);
        v.damping = LoadProperty<PxReal>(*attr, "Damping", 0.0f);
        return v;
    }

    PxBase* LoadMaterial(TiXmlElement& ele)
    {
        auto index = PR(Index, PxU32, 0);
        if (materials_.find(index) != materials_.end()) throw std::runtime_error("Duplicate material index");

        auto mat = physics_->createMaterial(
            PR(StaticFriction, PxReal, 1.0f),
            PR(DynamicFriction, PxReal, 1.0f),
            PR(Restitution, PxReal, 0.0f)
        );

        collection_->add(*mat);
        materials_.insert(std::make_pair(index, mat));
        return mat;
    }

    void LoadRigidActor(TiXmlElement& ele, PxRigidActor* o)
    {
        o->setName(_strdup(PR(Name, std::string, "").c_str()));
        o->setActorFlag(PxActorFlag::eDISABLE_GRAVITY, PFLAG(DisableGravity));
        o->setDominanceGroup((PxDominanceGroup)PR(DominanceGroup, PxU32, 0));

        auto shapes = ele.FirstChildElement("Shapes");
        if (shapes) {
            for (auto shapeEle = shapes->FirstChildElement("Shape"); shapeEle; shapeEle = shapeEle->NextSiblingElement("Shape")) {
                o->attachShape(*LoadShape(*shapeEle));
            }
        }

        actors_.insert(std::make_pair(o->getName(), o));
    }

    PxBase* LoadRigidStatic(TiXmlElement& ele)
    {
        auto o = physics_->createRigidStatic(
            P(GlobalPose, PxTransform)
        );

        LoadRigidActor(ele, o);

        collection_->add(*o);
        return o;
    }

    void LoadRigidBody(TiXmlElement& ele, PxRigidBody* o)
    {
        LoadRigidActor(ele, o);

        SET_P(CMassLocalPose, PxTransform);
        SET_PR(Mass, PxReal, 1.0f);
        SET_PR(MassSpaceInertiaTensor, PxVec3, PxVec3(1.0f, 1.0f, 1.0f));
        SET_PR(LinearDamping, PxReal, 0.0f);
        SET_PR(AngularDamping, PxReal, 0.5f);
        SET_PB(MaxLinearVelocity, 1e+16f);
        SET_PR(MaxAngularVelocity, PxReal, 100.0f);

        SET_FLAG(Kinematic, RigidBodyFlag, PxRigidBodyFlag::eKINEMATIC);
        SET_FLAG(EnableCCD, RigidBodyFlag, PxRigidBodyFlag::eENABLE_CCD);
        SET_FLAG(EnableCCDFriction, RigidBodyFlag, PxRigidBodyFlag::eENABLE_CCD_FRICTION);
        SET_FLAG(EnableSpeculativeCCD, RigidBodyFlag, PxRigidBodyFlag::eENABLE_SPECULATIVE_CCD);
        SET_FLAG(EnableCCDMaxContactImpulse, RigidBodyFlag, PxRigidBodyFlag::eENABLE_CCD_MAX_CONTACT_IMPULSE);
        SET_FLAG(RetainAccelerations, RigidBodyFlag, PxRigidBodyFlag::eRETAIN_ACCELERATIONS);

        SET_PR(MinCCDAdvanceCoefficient, PxReal, 0.15f);
        SET_PB(MaxDepenetrationVelocity, 1e+32f);
        SET_PB(MaxContactImpulse, 1e+32f);
    }

    PxBase* LoadRigidDynamic(TiXmlElement& ele)
    {
        auto o = physics_->createRigidDynamic(
            P(GlobalPose, PxTransform)
        );

        auto minPositionIters = PR(MinPositionIters, PxU32, 4);
        auto minVelocityIters = PR(MinVelocityIters, PxU32, 1);
        o->setSolverIterationCounts(minPositionIters, minVelocityIters);

        SET_PR(SleepThreshold, PxReal, 0.005f);
        SET_PR(StabilizationThreshold, PxReal, 0.0025f);
        
        SET_FLAG(LockLinearX, RigidDynamicLockFlag, PxRigidDynamicLockFlag::eLOCK_LINEAR_X);
        SET_FLAG(LockLinearY, RigidDynamicLockFlag, PxRigidDynamicLockFlag::eLOCK_LINEAR_Y);
        SET_FLAG(LockLinearZ, RigidDynamicLockFlag, PxRigidDynamicLockFlag::eLOCK_LINEAR_Z);
        SET_FLAG(LockAngularX, RigidDynamicLockFlag, PxRigidDynamicLockFlag::eLOCK_ANGULAR_X);
        SET_FLAG(LockAngularY, RigidDynamicLockFlag, PxRigidDynamicLockFlag::eLOCK_ANGULAR_Y);
        SET_FLAG(LockAngularZ, RigidDynamicLockFlag, PxRigidDynamicLockFlag::eLOCK_ANGULAR_Z);

        SET_PR(WakeCounter, PxReal, 0.0f);
        SET_PB(ContactReportThreshold, PX_MAX_F32);

        LoadRigidBody(ele, o);

        collection_->add(*o);
        return o;
    }

    PxShape* LoadShape(TiXmlElement& ele)
    {
        auto matIndex = PR(MaterialIndex, PxU32, 0);
        auto mat = materials_.find(matIndex);
        if (mat == materials_.end()) throw std::runtime_error("Shape references unknown material index");

        auto geomEle = ele.FirstChildElement("Geometry");
        if (!geomEle) throw std::runtime_error("Shape has no geometry");

        auto geom = LoadGeometry(*geomEle);

        auto o = physics_->createShape(*geom, *mat->second, true);
        o->setName(_strdup(PR(Name, std::string, "").c_str()));
        o->setLocalPose(P(LocalPose, PxTransform));
        o->setContactOffset(PR(ContactOffset, PxReal, 0.02f));
        o->setRestOffset(PR(RestOffset, PxReal, 0.0f));
        o->setTorsionalPatchRadius(PR(TorsionalPatchRadius, PxReal, 0.0f));
        o->setMinTorsionalPatchRadius(PR(MinTorsionalPatchRadius, PxReal, 0.0f));

        collection_->add(*o);
        return o;
    }

    PxGeometry* LoadSphere(TiXmlElement& ele)
    {
        return new PxSphereGeometry(
            PR(Radius, PxReal, 1.0f)
        );
    }

    PxGeometry* LoadCapsule(TiXmlElement& ele)
    {
        return new PxCapsuleGeometry(
            PR(Radius, PxReal, 1.0f),
            PR(HalfHeight, PxReal, 1.0f)
        );
    }

    PxGeometry* LoadBox(TiXmlElement& ele)
    {
        return new PxBoxGeometry(
            P(HalExtents, PxVec3)
        );
    }

    PxGeometry* LoadConvexMeshGeometry(TiXmlElement& ele)
    {
        auto meshEle = ele.FirstChildElement("ConvexMesh");
        if (!meshEle) throw std::runtime_error("Geometry has no ConvexMesh");
        auto mesh = LoadConvexMesh(ele);

        return new PxConvexMeshGeometry(
            mesh,
            P(Scale, PxMeshScale)
        );
    }

    PxGeometry* LoadTriangleMeshGeometry(TiXmlElement& ele)
    {
        auto meshEle = ele.FirstChildElement("TriangleMesh");
        if (!meshEle) throw std::runtime_error("Geometry has no TriangleMesh");
        auto mesh = LoadTriangleMesh(ele);

        return new PxTriangleMeshGeometry(
            mesh,
            P(Scale, PxMeshScale)
        );
    }

    PxConvexMesh* LoadConvexMesh(TiXmlElement& ele)
    {
        throw new std::runtime_error("LoadConvexMesh: Dont know how to do this yet");
    }

    PxTriangleMesh* LoadTriangleMesh(TiXmlElement& ele)
    {
        throw new std::runtime_error("LoadTriangleMesh: Dont know how to do this yet");
    }

    PxGeometry* LoadGeometry(TiXmlElement& ele)
    {
        auto type = PR(Type, std::string, "");

        if (type == "Sphere") {
            return LoadSphere(ele);
        } else if (type == "Capsule") {
            return LoadCapsule(ele);
        } else if (type == "Box") {
            return LoadBox(ele);
        } else if (type == "ConvexMesh") {
            return LoadConvexMeshGeometry(ele);
        } else if (type == "TriangleMesh") {
            return LoadTriangleMeshGeometry(ele);
        } else {
            throw std::runtime_error("Unknown geometry type");
        }
    }

    void LoadJoint(PxJoint* o, TiXmlElement& ele)
    {
        o->setName(_strdup(PR(Name, std::string, "").c_str()));

        auto force = P_BOUNDED(BreakForce, PxReal, PX_MAX_F32);
        auto torque = P_BOUNDED(BreakTorque, PxReal, PX_MAX_F32);
        o->setBreakForce(force, torque);

        SET_FLAG(ProjectToActor0, ConstraintFlag, PxConstraintFlag::ePROJECT_TO_ACTOR0);
        SET_FLAG(ProjectToActor1, ConstraintFlag, PxConstraintFlag::ePROJECT_TO_ACTOR1);
        SET_FLAG(CollisionEnabled, ConstraintFlag, PxConstraintFlag::eCOLLISION_ENABLED);
        SET_FLAG(DriveLimitsAreForces, ConstraintFlag, PxConstraintFlag::eDRIVE_LIMITS_ARE_FORCES);

        SET_PR(InvMassScale0, PxReal, 1.0f);
        SET_PR(InvInertiaScale0, PxReal, 1.0f);
        SET_PR(InvMassScale1, PxReal, 1.0f);
        SET_PR(InvInertiaScale1, PxReal, 1.0f);
    }

    PxBase* LoadD6Joint(TiXmlElement& ele)
    {
        auto actor0Name = P(Actor0, std::string);
        auto actor1Name = P(Actor1, std::string);

        auto actor0It = actors_.find(actor0Name);
        if (actor0It == actors_.end()) throw std::runtime_error("Actor0 has invalid name");

        auto actor1It = actors_.find(actor1Name);
        if (actor1It == actors_.end()) throw std::runtime_error("Actor1 has invalid name");

        auto pose0 = P(Actor0LocalPose, PxTransform);
        auto pose1 = P(Actor1LocalPose, PxTransform);

        auto o = PxD6JointCreate(*physics_, actor0It->second, pose0, actor1It->second, pose1);
        LoadJoint(o, ele);

        o->setMotion(PxD6Axis::eX, P(MotionX, PxD6Motion::Enum));
        o->setMotion(PxD6Axis::eY, P(MotionY, PxD6Motion::Enum));
        o->setMotion(PxD6Axis::eZ, P(MotionZ, PxD6Motion::Enum));
        o->setMotion(PxD6Axis::eTWIST, P(MotionTwist, PxD6Motion::Enum));
        o->setMotion(PxD6Axis::eSWING1, P(MotionSwing1, PxD6Motion::Enum));
        o->setMotion(PxD6Axis::eSWING2, P(MotionSwing2, PxD6Motion::Enum));

        SET_P(DistanceLimit, PxJointLinearLimit);
        o->setLinearLimit(PxD6Axis::eX, P(LinearLimitX, PxJointLinearLimitPair));
        o->setLinearLimit(PxD6Axis::eY, P(LinearLimitY, PxJointLinearLimitPair));
        o->setLinearLimit(PxD6Axis::eZ, P(LinearLimitZ, PxJointLinearLimitPair));
        SET_P(TwistLimit, PxJointAngularLimitPair);
        SET_P(SwingLimit, PxJointLimitCone);
        SET_P(PyramidSwingLimit, PxJointLimitPyramid);

        o->setDrive(PxD6Drive::eX, P(DriveX, PxD6JointDrive));
        o->setDrive(PxD6Drive::eY, P(DriveY, PxD6JointDrive));
        o->setDrive(PxD6Drive::eZ, P(DriveZ, PxD6JointDrive));
        o->setDrive(PxD6Drive::eSWING, P(DriveSwing, PxD6JointDrive));
        o->setDrive(PxD6Drive::eTWIST, P(DriveTwist, PxD6JointDrive));
        o->setDrive(PxD6Drive::eSLERP, P(DriveSlerp, PxD6JointDrive));

        SET_PR(ProjectionLinearTolerance, PxReal, 1e+10f);
        SET_PR(ProjectionAngularTolerance, PxReal, 3.14159f);

        collection_->add(*o->getConstraint());
        collection_->add(*o);
        return o;
    }

    PxBase* LoadArticulationJoint(TiXmlElement& ele, PxArticulationJoint* o)
    {
        SET_P(ParentPose, PxTransform);
        SET_P(ChildPose, PxTransform);

        SET_PR(Stiffness, PxReal, 0.0f);
        SET_PR(Damping, PxReal, 0.0f);
        SET_PR(InternalCompliance, PxReal, 0.0f);
        SET_PR(ExternalCompliance, PxReal, 0.0f);

        o->setSwingLimit(PR(SwingLimitZ, PxReal, PxPi / 4), PR(SwingLimitY, PxReal, PxPi / 4));

        SET_PR(TangentialStiffness, PxReal, 0.0f);
        SET_PR(TangentialDamping, PxReal, 0.0f);
        SET_PR(SwingLimitContactDistance, PxReal, 0.05f);
        SET_PR(SwingLimitEnabled, bool, false);

        o->setTwistLimit(PR(TwistLimitLower, PxReal, -PxPi / 4), PR(TwistLimitUpper, PxReal, PxPi / 4));

        SET_PR(TwistLimitContactDistance, PxReal, 0.05f);
        SET_PR(TwistLimitEnabled, bool, false);

        collection_->add(*o);
        return o;
    }

    PxBase* LoadArticulationLink(TiXmlElement& ele, PxArticulation& articulation, PxArticulationLink* parent)
    {
        auto o = articulation.createLink(
            parent, P(GlobalPose, PxTransform)
        );

        LoadRigidBody(ele, o);

        if (parent != nullptr) {
            auto jointNode = ele.FirstChildElement("Joint");
            if (jointNode == nullptr) throw std::runtime_error("Joint missing on articulation link");
            LoadArticulationJoint(*jointNode, static_cast<PxArticulationJoint*>(o->getInboundJoint()));
        }

        collection_->add(*o);

        auto linksNode = ele.FirstChildElement("Links");
        if (linksNode) {
            for (auto linkNode = linksNode->FirstChildElement("Link"); linkNode; linkNode = linkNode->NextSiblingElement("Link")) {
                LoadArticulationLink(*linkNode, articulation, o);
            }
        }

        return o;
    }

    PxBase* LoadArticulation(TiXmlElement& ele)
    {
        auto o = physics_->createArticulation();

        SET_PR(SleepThreshold, PxReal, 0.005f);
        SET_PR(StabilizationThreshold, PxReal, 0.0025f);
        SET_PR(WakeCounter, PxReal, 0.4f);

        SET_PR(MaxProjectionIterations, PxU32, 4);
        SET_PR(SeparationTolerance, PxReal, 0.01f);
        SET_PR(InternalDriveIterations, PxU32, 4);
        SET_PR(ExternalDriveIterations, PxU32, 4);

        auto linksNode = ele.FirstChildElement("Links");
        if (linksNode) {
            for (auto linkNode = linksNode->FirstChildElement("Link"); linkNode; linkNode = linkNode->NextSiblingElement("Link")) {
                LoadArticulationLink(*linkNode, *o, nullptr);

            }
        }

        collection_->add(*o);
        return o;
    }

    PxBase* LoadTopLevel(TiXmlElement& ele)
    {
        auto type = ele.ValueStr();
        if (type == "Material") {
            return LoadMaterial(ele);
        } else if (type == "RigidStatic") {
            return LoadRigidStatic(ele);
        } else if (type == "RigidDynamic") {
            return LoadRigidDynamic(ele);
        } else if (type == "D6Joint") {
            return LoadD6Joint(ele);
        } else if (type == "Articulation") {
            return LoadArticulation(ele);
        } else {
            std::cout << "WARNING: Don't know how to load object " << type << std::endl;
            return nullptr;
        }
    }
};


PxCollection* PhysXConverter::LoadCollectionFromXml(std::span<uint8_t> const& xml)
{
    PhysXLoader loader;
    loader.physics_ = physics_;
    loader.cooking_ = cooking_;

    // Ensure string is null-terminated
    std::string s((char const*)xml.data(), xml.size());

    TiXmlDocument doc;
    doc.Parse(s.c_str(), 0, TIXML_ENCODING_UTF8);
    if (doc.Error()) throw std::runtime_error(doc.ErrorDesc());

    auto root = doc.FirstChildElement();
    if (root == nullptr || strcmp(root->Value(), "BG3Physics") != 0) throw std::runtime_error("Expected a BG3Physics XML document");

    return loader.Load(*root);
}
