#include "PhysicsTool.h"
#include <cstdlib>


class PhysXExporterAllocator : public PxAllocatorCallback
{
public:
    void* allocate(size_t size, const char*, const char*, int)
    {
        void* ptr = _aligned_malloc(size, 16);
        memset(ptr, 0, size);
        return ptr;
    }

    void deallocate(void* ptr)
    {
        _aligned_free(ptr);
    }
};


PxDefaultErrorCallback gPxErrorCallback;
PhysXExporterAllocator gPxAllocator;

bool PhysXConverter::InitPhysX()
{
    foundation_ = PxCreateFoundation(PX_PHYSICS_VERSION, gPxAllocator,
        gPxErrorCallback);
    if (!foundation_) return false;

    physics_ = PxCreatePhysics(PX_PHYSICS_VERSION, *foundation_,
        PxTolerancesScale(), false, nullptr);
    if (!physics_) return false;

    cooking_ = PxCreateCooking(PX_PHYSICS_VERSION, *foundation_, PxCookingParams(PxTolerancesScale()));
    if (!cooking_) return false;

    if (!PxInitExtensions(*physics_, nullptr)) return false;

    registry_ = PxSerialization::createSerializationRegistry(PxGetPhysics());

    return true;
}


void PhysXConverter::ShutdownPhysX()
{
    if (cooking_ == nullptr) return;

    registry_->release();
    registry_ = nullptr;

    cooking_->release();
    cooking_ = nullptr;

    physics_->release();
    physics_ = nullptr;

    foundation_->release();
    foundation_ = nullptr;
}


PxCollection* PhysXConverter::LoadCollectionFromBinary(std::span<uint8_t> const& bin)
{
    auto binSize = bin.size();
    auto binInput = new uint8_t[binSize + PX_SERIAL_FILE_ALIGN];
    // TODO - release memory block after use
    void* memory128 = (void*)((uintptr_t(binInput) + PX_SERIAL_FILE_ALIGN) & ~(PX_SERIAL_FILE_ALIGN - 1));
    memcpy(memory128, bin.data(), binSize);

    return PxSerialization::createCollectionFromBinary(memory128, *registry_);
}


class KazMemoryOutputStream : public PxOutputStream
{
public:
    uint32_t write(const void* src, uint32_t count) override
    {
        auto p = (const uint8_t*)src;
        std::copy(p, p + count, std::back_inserter(buf_));
        return count;
    }

    inline std::vector<uint8_t> const& contents() const
    {
        return buf_;
    }

private:
    std::vector<uint8_t> buf_;
};


std::vector<uint8_t> PhysXConverter::SaveCollectionToBinary(PxCollection& collection)
{
    KazMemoryOutputStream outStream;
    if (!PxSerialization::serializeCollectionToBinaryDeterministic(outStream, collection, *registry_, nullptr, true)) return {};

    return outStream.contents();
}


std::vector<uint8_t> LoadFile(std::string const& path)
{
    std::vector<uint8_t> bin;
    std::ifstream f(path.c_str(), std::ios::binary | std::ios::in);
    if (!f.good()) throw std::runtime_error(std::string("Failed to open file: ") + path);

    f.seekg(0, std::ios::end);
    auto size = (std::streamoff)f.tellg();
    f.seekg(0, std::ios::beg);
    bin.resize(size);

    f.read(reinterpret_cast<char*>(bin.data()), bin.size());
    return bin;
}


void WriteFile(std::string const& path, std::vector<uint8_t> const& contents)
{
    std::ofstream f(path.c_str(), std::ios::binary | std::ios::out);
    if (!f.good()) throw std::runtime_error(std::string("Failed to open file for writing: ") + path);

    f.write(reinterpret_cast<char const*>(contents.data()), contents.size());
}


int main(int argc, char** argv)
{
    if (argc != 3) {
        std::cout << "Usage: PhysicsTool <input file> <output file>" << std::endl;
        return 1;
    }

    std::string inputPath = argv[1];
    std::string outputPath = argv[2];

    std::string inputExt = inputPath.length() > 4 ? inputPath.substr(inputPath.size() - 4) : "";
    std::string outputExt = outputPath.length() > 4 ? outputPath.substr(outputPath.size() - 4) : "";

    try {
        if (inputExt != ".bin" && inputExt != ".xml") throw std::runtime_error("Input file must be a .bin or .xml file");
        if (outputExt != ".bin" && outputExt != ".xml") throw std::runtime_error("Output file must be a .bin or .xml file");

        bool inputIsXml = (inputExt == ".xml");
        bool outputIsXml = (outputExt == ".xml");

        PhysXConverter converter;
        if (!converter.InitPhysX()) {
            std::cout << "Failed to initialize PhysX runtime" << std::endl;
            return 1;
        }

        auto input = LoadFile(inputPath);

        auto collection = inputIsXml ? converter.LoadCollectionFromXml(input) : converter.LoadCollectionFromBinary(input);
        if (!collection) throw std::runtime_error("Unable to load resource collection from source file");

        auto output = outputIsXml ? converter.SaveCollectionToXml(*collection) : converter.SaveCollectionToBinary(*collection);
        WriteFile(outputPath, output);
    } catch (std::exception& e) {
        std::cout << e.what() << std::endl;
        return 1;
    }

    return 0;
}
