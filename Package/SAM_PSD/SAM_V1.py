
#import torch
#import torchvision
#print("PyTorch version:", torch.__version__)
#print("Torchvision version:", torchvision.__version__)
#print("CUDA is available:", torch.cuda.is_available())
import argparse
import numpy as np
import torch
import matplotlib.pyplot as plt
import cv2
import time
import os
from pycocotools import mask as coco_mask
import pytoshop
from pytoshop import layers
from pytoshop.enums import BlendMode

from datetime import datetime

from segment_anything import sam_model_registry, SamAutomaticMaskGenerator, SamPredictor

def generate_random_color():
    return np.random.randint(0, 256), np.random.randint(0, 256), np.random.randint(0, 256)


def create_base_layer(image):
    rgba_image = cv2.cvtColor(image, cv2.COLOR_RGB2RGBA)
    return [rgba_image]


def create_mask_layers(image, masks):
    layer_list = []

    for result in masks:
        rle = result['segmentation']
        #print(rle)
        mask = rle.astype(np.uint8)
        #mask = coco_mask.decode(rle).astype(np.uint8)
        rgba_image = cv2.cvtColor(image, cv2.COLOR_RGB2RGBA)
        rgba_image[..., 3] = cv2.bitwise_and(rgba_image[..., 3], rgba_image[..., 3], mask=mask)

        layer_list.append(rgba_image)

    return layer_list


def create_mask_gallery(image, masks):
    mask_array_list = []
    label_list = []

    for index, result in enumerate(masks):
        rle = result['segmentation']
        mask = coco_mask.decode(rle).astype(np.uint8)

        rgba_image = cv2.cvtColor(image, cv2.COLOR_RGB2RGBA)
        rgba_image[..., 3] = cv2.bitwise_and(rgba_image[..., 3], rgba_image[..., 3], mask=mask)

        mask_array_list.append(rgba_image)
        label_list.append(f'Part {index}')

    return [[img, label] for img, label in zip(mask_array_list, label_list)]


def create_mask_combined_images(image, masks):
    final_result = np.zeros_like(image)

    for result in masks:
        rle = result['segmentation']
        mask = coco_mask.decode(rle).astype(np.uint8)

        color = generate_random_color()
        colored_mask = np.zeros_like(image)
        colored_mask[mask == 1] = color

        final_result = cv2.addWeighted(final_result, 1, colored_mask, 0.5, 0)

    combined_image = cv2.addWeighted(image, 1, final_result, 0.5, 0)
    return [combined_image, "masked"]


def insert_psd_layer(psd, image_data, layer_name, blending_mode):
    channel_data = [layers.ChannelImageData(image=image_data[:, :, i], compression=1) for i in range(4)]

    layer_record = layers.LayerRecord(
        channels={-1: channel_data[3], 0: channel_data[0], 1: channel_data[1], 2: channel_data[2]},
        top=0, bottom=image_data.shape[0], left=0, right=image_data.shape[1],
        blend_mode=blending_mode,
        name=layer_name,
        opacity=255,
    )
    psd.layer_and_mask_info.layer_info.layer_records.append(layer_record)
    return psd


def save_psd(input_image_data, layer_data, layer_names, blending_modes, output_path):
    psd_file = pytoshop.core.PsdFile(num_channels=3, height=input_image_data.shape[0], width=input_image_data.shape[1])
    psd_file.layer_and_mask_info.layer_info.layer_records.clear()

    for index, layer in enumerate(layer_data):
        psd_file = insert_psd_layer(psd_file, layer, layer_names[index], blending_modes[index])

    with open(output_path, 'wb') as output_file:
        psd_file.write(output_file)


def save_psd_with_masks(image, masks, output_path):
    original_layer = create_base_layer(image)
    mask_layers = create_mask_layers(image, masks)
    names = [f'Part {i}' for i in range(len(mask_layers))]
    modes = [BlendMode.normal] * (len(mask_layers)+1)
    save_psd(image, original_layer+mask_layers, ['Original_Image']+names, modes, output_path)
def show_anns(anns):
    if len(anns) == 0:
        return
    sorted_anns = sorted(anns, key=(lambda x: x['area']), reverse=True)
    ax = plt.gca()
    ax.set_autoscale_on(False)

    img = np.ones((sorted_anns[0]['segmentation'].shape[0], sorted_anns[0]['segmentation'].shape[1], 4))
    img[:,:,3] = 0
    for ann in sorted_anns:
        m = ann['segmentation']
        color_mask = np.concatenate([np.random.random(3), [0.35]])
        img[m] = color_mask
    ax.imshow(img)
  
if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--image", default="", type=str, help="image path")
    parser.add_argument("--device", default="cpu", type=str, help="cpu or cuda")
    parser.add_argument("--output", default="", type=str, help="Output Path")


    
    args = parser.parse_args()
    if(args.image==""):
        print("no image path")
        import sys
        sys.exit("Exiting the program")
        
    image = cv2.imread(args.image)
    image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
    
    #plt.figure(figsize=(20,20))
    #plt.imshow(image)
    #plt.axis('off')
    #plt.show()
    sam_checkpoint = "sam_vit_h_4b8939.pth"
    model_type = "vit_h"  
    sam = sam_model_registry[model_type](checkpoint=sam_checkpoint)
    if(args.device=="cuda"):
        device = "cuda"
        sam.to(device=device)
    else:
        device = "cpu"
        sam.to(device=device)
    
    mask_generator = SamAutomaticMaskGenerator(sam)
    
    start_time = time.time()
    
    print("start mask_generator")
    masks = mask_generator.generate(image)
    
    end_time = time.time()


    execution_time = end_time - start_time
    print(f"generator time: {execution_time} s")
    #print(len(masks))
    #print(masks[0].keys())
    
    
    
    #plt.figure(figsize=(20,20))
    #plt.imshow(image)
    #show_anns(masks)
    #plt.axis('off')
    #plt.show() 
    if not os.path.exists("PSD"):
        os.makedirs("PSD")
    
    timestamp = datetime.now().strftime("%m%d%H%M%S")
    #base_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), "PSD"))
    output_path = args.output #os.path.join(base_dir, f"temp.psd")
    print("Save to PSD")

    save_psd_with_masks(image,masks,output_path)
    print("End")
