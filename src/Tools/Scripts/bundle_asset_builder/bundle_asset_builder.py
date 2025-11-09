import sys
import os
import csv
import json
import shutil

from PIL import ImageFont, ImageDraw, Image

CATALOG_PATHS = [
    "../../../MHServerEmu.Games/Data/Game/MTXStore/Catalog.json",
    "../../../MHServerEmu.Games/Data/Game/MTXStore/CatalogRestoredHeroes.json",
]

INFO_TEMPLATE = "info.html"
INFO_NAMES_TABLE = "names.tsv"
IMAGE_TEMPLATE = "image.png"
IMAGE_FONT = "BebasNeue-Regular.ttf"
IMAGE_FONT_SIZE = 32
CSS_FILE = "style.css"

INFO_OUTPUT_DIRECTORY = "bundles"
IMAGE_OUTPUT_DIRECTORY = "bundles/images"
CSS_OUTPUT_DIRECTORY = "bundles/css"

def build_info(entry: dict, template: str, names: dict):
    # Replace placeholders with catalog data
    items = ""
    for item in entry["GuidItems"]:
        # Fall back to the id itself if no name is available.
        item_id = str(item['ItemPrototypeRuntimeIdForClient'])
        item_name = names.get(item_id, item_id)

        # Do not specify quantity if it's only one thing.
        quantity = item['Quantity']
        if quantity == 1:
            items += f"<li>{item_name}</li>"
        else:
            items += f"<li>{item_name} x{quantity}</li>"

    info = template.format(
        TITLE=entry["LocalizedEntries"][0]["Title"],
        ITEMS=items,
        SKU_ID=f"0x{entry['SkuId']:X}",
        PRICE=str(entry["LocalizedEntries"][0]["ItemPrice"])
    )

    file_name = os.path.basename(entry["InfoUrls"][0]["Url"])
    file_path = os.path.join(INFO_OUTPUT_DIRECTORY, file_name)
    os.makedirs(INFO_OUTPUT_DIRECTORY, exist_ok=True)

    with open(file_path, 'w') as file:
        file.write(info)

    print(file_name)

def build_image(entry: dict, template: Image, font: ImageFont):
    file_name = os.path.basename(entry["ContentData"][0]["Url"])

    image_text = (str(file_name)
        .replace("MTXStore_Bundle_", "")
        .replace("MTX_Store_Bundle_", "")
        .replace("Thumb_", "")
        .replace("_Thumb", "")
        .replace("Thumb", "")
        .replace(".png", "")
    )

    image = template.copy()
    draw = ImageDraw.Draw(image)

    _, _, text_width, text_height = draw.textbbox((0, 0), image_text, font=font)

    text_x = (image.width - text_width) / 2
    text_y = (image.height - text_height) / 2

    draw.text((text_x + 2, text_y + 2), image_text, font=font, fill="black")
    draw.text((text_x, text_y), image_text, font=font, fill="white")

    os.makedirs(IMAGE_OUTPUT_DIRECTORY, exist_ok=True)
    file_path = os.path.join(IMAGE_OUTPUT_DIRECTORY, file_name)
    image.save(file_path)

    print(file_name)

def build_css():
    # No dynamic building happening here, just copy the premade style sheet.
    os.makedirs(CSS_OUTPUT_DIRECTORY, exist_ok=True)
    file_path = os.path.join(CSS_OUTPUT_DIRECTORY, CSS_FILE)
    shutil.copy(CSS_FILE, file_path)

def main(args: list[str]):
    info_template = ""
    with open(INFO_TEMPLATE) as file:
        info_template = file.read()

    image_template = Image.open(IMAGE_TEMPLATE)
    image_font = ImageFont.truetype(IMAGE_FONT, IMAGE_FONT_SIZE)

    names = {}
    with open(INFO_NAMES_TABLE) as file:
        for row in csv.reader(file, delimiter='\t'):
            names[row[0]] = row[1]

    for catalog_path in CATALOG_PATHS:
        catalog = {}
        with open(catalog_path) as file:
            catalog = json.load(file)

        for entry in catalog:
            if len(entry["InfoUrls"]) > 0:
                build_info(entry, info_template, names)

            if len(entry["ContentData"]) > 0:
                build_image(entry, image_template, image_font)

    build_css()

if (__name__ == "__main__"):
    main(sys.argv)