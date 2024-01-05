from PIL import Image
import torch
from torchvision import transforms
from torchvision.transforms.functional import InterpolationMode
import os
from pathlib import Path
from models.blip import blip_decoder
import argparse
import requests

#pip install transformers==4.15.0 timm==0.4.12 fairscale==0.4.4 pycocoevalcap
def folder_exists(folde_path):
    return os.path.exists(folde_path)
def load_demo_image(image_size,device,path):
    img_path = path
    raw_image = Image.open(img_path).convert('RGB')    
    transform = transforms.Compose([
        transforms.Resize((image_size,image_size),interpolation=InterpolationMode.BICUBIC),
        transforms.ToTensor(),
        transforms.Normalize((0.48145466, 0.4578275, 0.40821073), (0.26862954, 0.26130258, 0.27577711))
        ]) 
    image = transform(raw_image).unsqueeze(0).to(device)   
    return image


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--image", default="", type=str, help="image path")
    parser.add_argument("--device", default="cuda", type=str, help="cpu or cuda")    
    args = parser.parse_args()


    #device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    device = torch.device('cpu')
    print(device)
    image_size = 512
    image = load_demo_image(image_size=image_size, device=device,path=args.image)

    if folder_exists("./checkpoints"):
        model_url = 'checkpoints/model_base_capfilt_large.pth'#
    else:
        model = 'https://storage.googleapis.com/sfr-vision-language-research/BLIP/models/model_base_capfilt_large.pth'#checkpoints/model_base_capfilt_large.pth
        model_local_path = './checkpoints/model_base_capfilt_large.pth'
        print('Download model')
        response = requests.get(model)
        
        if response.status_code == 200:
            os.makedirs(os.path.dirname(model_local_path), exist_ok=True)
            print('Download model finish')
            with open(model_local_path, 'wb') as file:
                file.write(response.content)
            model_url = 'checkpoints/model_base_capfilt_large.pth'    
        else:
            print(f"Failed to download the model. HTTP Status Code: {response.status_code}")
    model = blip_decoder(pretrained=model_url, image_size=image_size, vit='base', med_config=os.path.join("configs", "med_config.json"))
    model.eval()
    model = model.to(device)
    
    with torch.no_grad():
    
        caption = model.generate(image, sample=False, num_beams=1, max_length=60, min_length=24) 
        print('caption: '+caption[0])
        with open('tagger_prompt.txt', 'w') as file:
            file.write(caption[0])



