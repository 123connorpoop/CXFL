#ifndef LAYER_H
#define LAYER_H
#include "pugixml.hpp"
#include "Frame.h"
#include <vector>
#include <memory>
class Layer {
	friend class Timeline;
private:
	pugi::xml_node root;
	void loadFrames(pugi::xml_node& layerNode);
	bool insertKeyframe(unsigned int frameIndex, bool isBlank);
	std::vector<std::unique_ptr<Frame>> frames;
	std::string color;
	std::string layerType;
	std::optional<unsigned int> parentLayerIndex;
	bool locked;
	std::string name;
public:
	Layer(pugi::xml_node& layerNode);
	~Layer();
	bool insertKeyframe(unsigned int frameIndex);
	bool insertBlankKeyframe(unsigned int frameIndex);
	Frame* getKeyFrame(unsigned int index);
	unsigned int getKeyframeIndex(unsigned int frameIndex);
	Frame* getFrame(unsigned int frameIndex);
	std::string getColor() const;
	void setColor(const std::string& color);
	std::string getLayerType() const;
	void setLayerType(const std::string& layerType);
	bool isLocked() const;
	void setLocked(bool locked);
	std::string getName() const;
	void setName(const std::string& name);
	std::optional<unsigned int> getParentLayerIndex() const;
	void setParentLayerIndex(std::optional<unsigned int> parentLayer);
	unsigned int getFrameCount();
	pugi::xml_node& getRoot();
};
const std::vector<std::string_view> ACCEPTABLE_LAYER_TYPES = { "normal", "guide", "guided", "mask", "masked", "folder" };
#endif // LAYER_H